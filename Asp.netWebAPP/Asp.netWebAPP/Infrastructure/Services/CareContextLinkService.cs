using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.Interface;
using Asp.netWebAPP.Infrastructure.Data;
using Asp.netWebAPP.Core.Shared.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Asp.netWebAPP.Core.Domain.Model;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Asp.netWebAPP.Infrastructure.Services
{

    public class CareContextLinkService : ICareContextLinkService
    {
        private readonly HttpClient _httpClient;
        private readonly AbdmDbContext _dbContext;
        private readonly IAbhaAuthService _authService;
        private readonly DanpheDbContext _danpheDbContext;
        private readonly AbdmAzureDbContext _dbContextAzure;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<CareContextLinkService> _logger;

        public CareContextLinkService(
            HttpClient httpClient,
            AbdmDbContext dbContext,
            IAbhaAuthService authService,
            DanpheDbContext danpheDbContext,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<CareContextLinkService> logger,
            AbdmAzureDbContext dbContextAzure)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _authService = authService;
            _danpheDbContext = danpheDbContext;
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
            _dbContextAzure = dbContextAzure;
        }

        /// <summary>
        /// Handles the process of linking patient care context with ABDM system.
        /// </summary>
        /// <param name="request">Care Context link request payload.</param>
        /// <returns>Response indicating success or failure of care context linking.</returns>
        public async Task<CareContextLinkResponseDTO> LinkCareContextAsync(CareContextLinkRequestDTO request)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(request.AbhaAddress))
                {
                    request.AbhaAddress = request.AbhaAddress.Trim().ToLowerInvariant();
                }
                else
                {
                    throw new AbdmInvalidCareContextRequestException("ABHA Address is missing or invalid.");
                }

                var config = await _authService.GetAbdmConfigAsync();
                var accessToken = await _authService.GetAccessTokenAsync();

                PatientModel existingPatient;
                try
                {
                    existingPatient = await _danpheDbContext.Patient
                        .FirstOrDefaultAsync(p => p.EHRNumber == request.AbhaNumber);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database query failed while searching for patient {AbhaNumber}.", request.AbhaNumber);
                    throw new DatabaseAccessException("Failed to access local patient database.", ex);
                }

                if (existingPatient == null)
                    throw new AbdmPatientNotFoundException("Patient not found with the provided ABHA Number in the local EHR system.");


                string fullName = string.Join(" ",
                    new[] { existingPatient.FirstName, existingPatient.MiddleName, existingPatient.LastName }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));

                if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
                {
                    _logger.LogWarning("Local patient record (EHR {EHR}) is missing sufficient name data for ABDM request.", existingPatient.EHRNumber);
                    throw new AbdmPatientDataIncompleteException("Local patient record is incomplete. Cannot proceed with Care Context linking due to missing/invalid Name data.");
                }

                string genderShort = existingPatient.Gender?.Trim().ToLower() switch
                {
                    "male" => "M",
                    "female" => "F",
                    _ => "U"
                };

                string yearOfBirth = existingPatient.DateOfBirth.Year.ToString();

                AbdmPatientLinkToken tokenRecord;
                try
                {
                    tokenRecord = await _dbContextAzure.AbdmPatientLinkTokens
                        .Where(t => t.AbhaAddress == request.AbhaAddress)
                        .OrderByDescending(t => t.CreatedOn)
                        .FirstOrDefaultAsync();
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database query failed while retrieving existing link token for {AbhaAddress}.", request.AbhaAddress);
                    throw new DatabaseAccessException("Failed to access token database.", ex);
                }


                string linkToken = null;

                if (tokenRecord != null)
                {
                    if (!string.IsNullOrWhiteSpace(tokenRecord.LinkToken) &&
                        tokenRecord.LinkTokenExpiry > DateTime.UtcNow)
                    {
                        linkToken = tokenRecord.LinkToken;
                    }
                    else
                    {
                        // Token found but expired
                        throw new AbdmLinkTokenExpiredException($"Existing Link Token for {request.AbhaAddress} has expired at {tokenRecord.LinkTokenExpiry}.");
                    }
                }

                else // tokenRecord is null, need to generate new token
                {
                    //  Generate new link token 
                    var generateRequest = new GenerateLinkTokenRequestDTO
                    {
                        abhaNumber = request.AbhaNumber,
                        abhaAddress = request.AbhaAddress,
                        name = fullName,
                        gender = genderShort,
                        yearOfBirth = yearOfBirth
                    };

                    var newTokenResponse = await GenerateLinkTokenAsync(generateRequest);

                    if (newTokenResponse.Status)
                    {
                        _logger.LogInformation("Link Token request 202 Accepted. Checking for immediate callback data for {AbhaAddress}.", request.AbhaAddress);

                        AbdmCallbackLog callbackData;
                        try
                        {
                            callbackData = await _dbContextAzure.AbdmCallbackLogs
                                .Where(c => c.AbhaAddress == request.AbhaAddress)
                                .OrderByDescending(c => c.ReceivedOn)
                                .FirstOrDefaultAsync();
                        }
                        catch (DbUpdateException ex)
                        {
                            _logger.LogError(ex, "Database query failed while retrieving callback log for {AbhaAddress}.", request.AbhaAddress);
                            throw new DatabaseAccessException("Failed to access callback log database.", ex);
                        }


                        if (callbackData != null && !string.IsNullOrWhiteSpace(callbackData.RawJsonPayload))
                        {
                            try
                            {
                                // Parse the linkToken from the asynchronous callback JSON structure
                                using var document = JsonDocument.Parse(callbackData.RawJsonPayload);
                                var root = document.RootElement;

                                if (root.TryGetProperty("linkToken", out JsonElement linkTokenElement) && linkTokenElement.ValueKind == JsonValueKind.String)
                                {
                                    linkToken = linkTokenElement.GetString();

                                    if (!string.IsNullOrWhiteSpace(linkToken))
                                    {
                                        _logger.LogInformation("Retrieved Link Token from callback log for {AbhaAddress} and saving.", request.AbhaAddress);

                                        var newCallbackTokenRecord = new AbdmPatientLinkToken
                                        {
                                            AbhaAddress = request.AbhaAddress,
                                            LinkToken = linkToken,
                                            LinkTokenExpiry = DateTime.UtcNow.AddMonths(6),
                                            CreatedOn = DateTime.UtcNow,
                                        };

                                        try
                                        {
                                            _dbContextAzure.AbdmPatientLinkTokens.Add(newCallbackTokenRecord);
                                            await _dbContextAzure.SaveChangesAsync();
                                        }
                                        catch (DbUpdateException ex)
                                        {
                                            _logger.LogError(ex, "Failed to save new link token for {AbhaAddress}.", request.AbhaAddress);
                                            throw new DatabaseAccessException("Failed to save new link token to database.", ex);
                                        }
                                    }
                                }
                            }
                            catch (JsonException ex) 
                            {
                                _logger.LogError(ex, "Failed to parse callback JSON for ABHA Address: {AbhaAddress}.", request.AbhaAddress);
                                throw new AbdmInvalidCareContextRequestException("Failed to parse asynchronous link token response from ABDM callback.", ex);
                            }
                        }


                    }
                    // else: GenerateLinkTokenAsync should have thrown an exception for non-202 status.
                }

                if (string.IsNullOrWhiteSpace(linkToken))
                    throw new AbdmLinkTokenGenerationException($"Fatal: No valid link token found or retrieved for ABHA address: {request.AbhaAddress}.");

                if (string.IsNullOrWhiteSpace(config?.careContextLinkUrl))
                {
                    _logger.LogError("ABDM Configuration Error: careContextLinkUrl is missing or empty.");
                    throw new AbdmConfigMissingException("The required ABDM endpoint URL for care context linking is missing from configuration.");
                }

                if (string.IsNullOrWhiteSpace(config?.hipId))
                {
                    _logger.LogError("ABDM Configuration Error: HIP ID is missing or empty.");
                    throw new AbdmConfigMissingException("The required HIP ID is missing from configuration.");
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

                    throw new AbdmExternalApiException($"ABDM API returned {response.StatusCode}: {content}");
                }

                return new CareContextLinkResponseDTO
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Status = "Success",
                    Message = "Care context linked successfully."
                };
            }
            catch (AbdmPatientNotFoundException)
            {
                throw;
            }
            catch (AbdmLinkTokenGenerationException)
            {
                throw;
            }
            catch (AbdmExternalApiException)
            {
                throw;
            }
            catch (AbdmInvalidCareContextRequestException)
            {
                throw;
            }
            catch (DatabaseAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ABDM M2: Unexpected error during Care Context linking.");
                throw new AbdmM2Exception("An unexpected internal error occurred while linking Care Context.", ex);
            }
        }

        private async Task<GenerateLinkTokenResponseDTO> GenerateLinkTokenAsync(GenerateLinkTokenRequestDTO request)
        {
            try
            {
                var config = await _authService.GetAbdmConfigAsync();
                var token = await _authService.GetAccessTokenAsync();

                if (string.IsNullOrWhiteSpace(config?.generateLinkTokenUrl))
                {
                    _logger.LogError("ABDM Configuration Error: generateLinkTokenUrl is missing or empty.");
                    throw new AbdmConfigMissingException("The required ABDM endpoint URL for Link Token generation is missing from configuration.");
                }
                if (string.IsNullOrWhiteSpace(config?.hipId))
                {
                    _logger.LogError("ABDM Configuration Error: HIP ID is missing or empty.");
                    throw new AbdmConfigMissingException("The required HIP ID is missing from configuration.");
                }

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


                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    _logger.LogWarning("ABDM Generate Link Token Request ACCEPTED (202). Token expected via callback.");
                    return new GenerateLinkTokenResponseDTO { Status = true };
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogError("ABDM Generate Link Token returned UNEXPECTED SUCCESS status {StatusCode}. Expected 202 Accepted. Treating as external failure.", response.StatusCode);
                    throw new AbdmExternalApiException($"ABDM token API returned unexpected success status: {response.StatusCode}. Expected 202 Accepted.");
                }
                else
                {
                    _logger.LogError(@"Generate Link Token API failed. Status: {StatusCode}. Response: {Content}",
                        response.StatusCode, content);
                    throw new AbdmExternalApiException($"ABDM token API error: {response.StatusCode} - {content}");
                }

            }

            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize Link Token request body.");
                throw new AbdmInvalidCareContextRequestException("Error serializing request for Link Token API.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during Link Token API call.");
                throw new AbdmExternalApiException("Failed to connect to ABDM Link Token API (Network/DNS/Connection issue).", ex);
            }
            catch (AbdmConfigMissingException)
            {
                throw;
            }
            catch (AbdmExternalApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ABDM M2: Unexpected internal error during Link Token generation.");
                throw new AbdmLinkTokenGenerationException("An unexpected error occurred while generating Link Token.", ex);
            }
        }
    }
}