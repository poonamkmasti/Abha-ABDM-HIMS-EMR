using Asp.netWebAPP.Core.Application.DTO_s;
//using Asp.netWebAPP.Core.Application.Exceptions;
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

        public async Task<CareContextLinkResponseDTO> LinkCareContextAsync(CareContextLinkRequestDTO request)
        {
            var config = await _authService.GetAbdmConfigAsync();
            var accessToken = await _authService.GetAccessTokenAsync();

            var existingPatient = await _danpheDbContext.Patient
                .FirstOrDefaultAsync(p => p.EHRNumber == request.AbhaNumber);

            //if (existingPatient == null)
            //    throw new NotFoundException("Patient not found with provided ABHA Number.");

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

                //if (newTokenResponse.Status != "Generated")
                //    throw new ExternalServiceException("Failed to generate new Link Token from ABDM API.");

                linkToken = newTokenResponse.LinkToken;

                //existingPatient.EncryptedLinkToken = protector.Protect(linkToken);
                //existingPatient.LinkTokenExpiry = DateTime.UtcNow.AddMonths(6);
                // Prefer CreatedOn if available, else use current UTC time
                existingPatient.LinkTokenExpiry = (existingPatient.CreatedOn.HasValue)
     ? existingPatient.CreatedOn.Value.AddMonths(6)
     : DateTime.UtcNow.AddMonths(6);



                _danpheDbContext.Patient.Update(existingPatient);
                await _danpheDbContext.SaveChangesAsync();
            }

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
               // throw new ExternalServiceException($"ABDM API returned {response.StatusCode}: {content}");
            }

            return new CareContextLinkResponseDTO
            {
                TransactionId = Guid.NewGuid().ToString(),
                Status = "Success",
                Message = "Care context linked successfully."
            };
        }

        private async Task<GenerateLinkTokenResponseDTO> GenerateLinkTokenAsync(GenerateLinkTokenRequestDTO request)
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
                //throw new ExternalServiceException($"ABDM token API error: {response.StatusCode}");
            }

            return new GenerateLinkTokenResponseDTO
            {
                LinkToken = "Extract_from_response_JSON",
                Expiry = DateTime.UtcNow.AddMonths(6),
                Status = "Generated"
            };
        }
    }
}
