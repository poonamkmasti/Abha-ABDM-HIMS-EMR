using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.Interface;
using Asp.netWebAPP.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Asp.netWebAPP.Infrastructure.Services
{
    public class CareContextLinkService : ICareContextLinkService
    {
        private readonly HttpClient _httpClient;
        private readonly AbdmDbContext _dbContext;
        private readonly IAbhaAuthService _authService;
        private readonly DanpheDbContext _danpheDbContext;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<CareContextLinkService> _logger; 

        public CareContextLinkService(
            HttpClient httpClient,
            AbdmDbContext dbContext,
            IAbhaAuthService authService,
            DanpheDbContext danpheDbContext,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<CareContextLinkService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _authService = authService;
            _danpheDbContext = danpheDbContext;
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        /// <summary>
        /// Links care context for a given patient ABHA number.
        /// Generates or reuses a valid Link Token and calls ABDM API.
        /// </summary>
        /// <param name="request">Request containing ABHA details</param>
        /// <returns>CareContextLinkResponseDTO with status and message</returns>
        public async Task<CareContextLinkResponseDTO> LinkCareContextAsync(CareContextLinkRequestDTO request)
        {
            try
            {
                var config = await _authService.GetAbdmConfigAsync();
                var accessToken = await _authService.GetAccessTokenAsync();

                //  STEP 1: Fetch patient
                var existingPatient = await _danpheDbContext.Patient
                    .FirstOrDefaultAsync(p => p.EHRNumber == request.AbhaNumber);

                if (existingPatient == null)
                    throw new InvalidOperationException("Patient not found with provided ABHA Number.");

                // Construct patient details
                string fullName = string.Join(" ",
                    new[] { existingPatient.FirstName, existingPatient.MiddleName, existingPatient.LastName }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));

                string genderShort = existingPatient.Gender?.Trim().ToLower() switch
                {
                    "male" => "M",
                    "female" => "F",
                    _ => "U"
                };

                string yearOfBirth = existingPatient.DateOfBirth.Year.ToString();
                var protector = _dataProtectionProvider.CreateProtector("LinkToken");

                string linkToken;

                //  STEP 2: Use existing token if valid, else generate new one
                if (!string.IsNullOrWhiteSpace(existingPatient.EncryptedLinkToken) &&
                    existingPatient.LinkTokenExpiry.HasValue &&
                    existingPatient.LinkTokenExpiry.Value > DateTime.UtcNow)
                {
                    linkToken = protector.Unprotect(existingPatient.EncryptedLinkToken);
                }
                else
                {
                    var generateRequest = new GenerateLinkTokenRequestDTO
                    {
                        AbhaNumber = request.AbhaNumber,
                        AbhaAddress = request.AbhaAddress,
                        Name = fullName,
                        Gender = genderShort,
                        YearOfBirth = yearOfBirth
                    };

                    var newTokenResponse = await GenerateLinkTokenAsync(generateRequest);

                    if (newTokenResponse.Status != "Generated")
                        throw new Exception("Failed to generate new Link Token from ABDM API.");

                    linkToken = newTokenResponse.LinkToken;

                    // Encrypt and save new token
                    existingPatient.EncryptedLinkToken = protector.Protect(linkToken);
                    existingPatient.LinkTokenExpiry = DateTime.UtcNow.AddMonths(6);

                    _danpheDbContext.Patient.Update(existingPatient);
                    await _danpheDbContext.SaveChangesAsync();
                }

                //  STEP 3: Call ABDM Care Context Link API
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.careContextLinkUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                httpRequest.Headers.Add("REQUEST-ID", Guid.NewGuid().ToString());
                httpRequest.Headers.Add("TIMESTAMP", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                httpRequest.Headers.Add("X-CM-ID", "sbx");
                httpRequest.Headers.Add("X-HIP-ID", config.hipId);
                httpRequest.Headers.Add("X-LINK-TOKEN", linkToken);

                var response = await _httpClient.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Care Context Link API failed. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, content);
                }

                return new CareContextLinkResponseDTO
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Status = response.IsSuccessStatusCode ? "Success" : "Failed",
                    Message = content
                };
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error while linking care context.");
                throw new Exception("Error while calling ABDM service. Please try again later.", httpEx);
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogWarning(invEx, "Invalid operation during LinkCareContextAsync.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in LinkCareContextAsync.");
                throw new Exception("An unexpected error occurred while linking care context.", ex);
            }
        }

        /// <summary>
        /// Generates a new link token using ABDM API.
        /// </summary>
        private async Task<GenerateLinkTokenResponseDTO> GenerateLinkTokenAsync(GenerateLinkTokenRequestDTO request)
        {
            try
            {
                var config = await _authService.GetAbdmConfigAsync();
                var token = await _authService.GetAccessTokenAsync();

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.generateLinkTokenUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpRequest.Headers.Add("REQUEST-ID", Guid.NewGuid().ToString());
                httpRequest.Headers.Add("TIMESTAMP", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                httpRequest.Headers.Add("X-CM-ID", "sbx");
                httpRequest.Headers.Add("X-HIP-ID", config.hipId);

                var response = await _httpClient.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Generate Link Token API failed. Status: {StatusCode}, Response: {Content}",
                        response.StatusCode, content);
                    return new GenerateLinkTokenResponseDTO
                    {
                        Status = "Failed",
                    };
                }

                return new GenerateLinkTokenResponseDTO
                {
                    LinkToken = "Extract_from_response_JSON",
                    Expiry = DateTime.UtcNow.AddMonths(6),
                    Status = "Generated"
                };
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error while generating Link Token.");
                throw new Exception("Network error occurred while generating link token.", httpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GenerateLinkTokenAsync.");
                throw new Exception("An unexpected error occurred while generating link token.", ex);
            }
        }
    }
}
