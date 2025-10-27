using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.Interface;
using Asp.netWebAPP.Core.Application.M2.Commands;
using Asp.netWebAPP.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Asp.netWebAPP.Core.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Asp.netWebAPP.Infrastructure.Services
{
    public class BridgeService : IBridgeService
    {
        private readonly HttpClient _httpClient;
        private readonly AbdmDbContext _dbContext;
        private readonly IAbhaAuthService _authService;
        private readonly AbdmAzureDbContext _dbContextAzure;
        private readonly ILogger<BridgeService> _logger;

        public BridgeService(HttpClient httpClient,
                             AbdmDbContext dbContext,
                             IAbhaAuthService authService,
                             AbdmAzureDbContext abdmAzureDbContext,
                             ILogger<BridgeService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _authService = authService;
            _dbContextAzure = abdmAzureDbContext;
            _logger = logger;
        }

        public async Task<bool> SaveCallbackAsync(JsonElement payload, string callbackUrl)
        {
            try
            {
                string jsonText = payload.GetRawText();
                string abhaAddress = ExtractAbhaAddress(payload);

                var callbackLog = new AbdmCallbackLog
                {
                    AbhaAddress = abhaAddress,
                    CallbackUrl = callbackUrl,
                    RawJsonPayload = jsonText,
                    ReceivedOn = DateTime.UtcNow
                };

                await _dbContextAzure.AbdmCallbackLogs.AddAsync(callbackLog);
                await _dbContextAzure.SaveChangesAsync();

                _logger.LogInformation(" Callback stored successfully for {AbhaAddress}", abhaAddress);

                
                // If callback URL ends with /hip/token/on-generate-token, store link token
                if (callbackUrl.EndsWith("/hip/token/on-generate-token", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string? linkToken = null;

               
                        if (payload.TryGetProperty("linkToken", out JsonElement tokenElement))
                        {
                            linkToken = tokenElement.GetString();
                        }

                        if (!string.IsNullOrEmpty(linkToken))
                        {
                            var tokenEntry = new AbdmPatientLinkToken
                            {
                                AbhaAddress = abhaAddress,
                                LinkToken = linkToken,
                                LinkTokenExpiry = DateTime.UtcNow.AddMonths(6),
                                CreatedOn = DateTime.UtcNow
                            };

                            await _dbContextAzure.AbdmPatientLinkTokens.AddAsync(tokenEntry);
                            await _dbContextAzure.SaveChangesAsync();

                            _logger.LogInformation(" Link token saved successfully for {AbhaAddress}", abhaAddress);
                        }
                        else
                        {
                            _logger.LogWarning(" No linkToken found in callback for {AbhaAddress}", abhaAddress);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, " Failed to process LinkToken callback for {AbhaAddress}", abhaAddress);
                        throw;                
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to save ABDM callback JSON. URL: {CallbackUrl}", callbackUrl);
                return false;
            }
        }
        private string ExtractAbhaAddress(JsonElement payload)
        {
            try
            {
                if (payload.TryGetProperty("abhaAddress", out JsonElement abhaAddress))
                {
                    return abhaAddress.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Could not extract AbhaAddress from payload.");
            }

            return string.Empty;
        }
    
        public async Task<RegisterBridgeServiceResponseDTO> RegisterBridgeServiceAsync(RegisterBridgeServiceCommand command)
        {
            var config = await _authService.GetAbdmConfigAsync();
            var token = await _authService.GetAccessTokenAsync();

            var url = config.registerBridgeServiceUrl; 

            var json = JsonSerializer.Serialize(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Bridge service registration failed. StatusCode: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return new RegisterBridgeServiceResponseDTO
                {
                    Success = false,
                    Status = (int)response.StatusCode,
                    Message = responseContent
                };
            }

            _logger.LogInformation("Bridge service registration success: {Response}", responseContent);

            return new RegisterBridgeServiceResponseDTO
            {
                Success = true,
                Status = (int)response.StatusCode,
                Data = responseContent
            };
        }

       
    }


}
