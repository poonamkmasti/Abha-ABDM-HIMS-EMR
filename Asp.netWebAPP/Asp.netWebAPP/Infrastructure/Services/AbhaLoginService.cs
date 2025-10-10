using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.Interface;
using Asp.netWebAPP.Core.Domain.Model;
using Asp.netWebAPP.Core.Domain.Value_Objects;
using Asp.netWebAPP.Infrastructure.Data;
using Asp.netWebAPP.Infrastructure.Security;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using VerifyOtpResponse = Asp.netWebAPP.Core.Application.DTO_s.VerifyOtpResponse;
using Microsoft.EntityFrameworkCore;
using Asp.netWebAPP.Core.Shared.Exceptions;

namespace Asp.netWebAPP.Infrastructure.Services
{
    public class Abhalogin_Service : IAbhaLoginService
    {
        private readonly AbdmDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly DanpheDbContext _DbContext;
        private readonly IAbhaAuthService _authService;

        public Abhalogin_Service(AbdmDbContext dbContext,
                                 HttpClient httpClient,
                                 DanpheDbContext DbContext,
                                 IAbhaAuthService authService)
        {
            _dbContext = dbContext;
            _httpClient = httpClient;
            _DbContext = DbContext;
            _authService = authService;
        }
        // Fetches ABDM configuration parameters from the database
        private async Task<AbdmConfigDTO> GetAbdmConfigAsync()
        {
            var row = await _dbContext.AbdmCore_Parameters
                .FirstOrDefaultAsync(p => p.ParameterGroupName == "ABDM");

            if (row == null)
                throw new Exception("ABDM_Config row not found in Core_Parameters");

            return JsonSerializer.Deserialize<AbdmConfigDTO>(row.ParameterValue)
                   ?? throw new Exception("Failed to deserialize ABDM config.");
        }
        public async Task<List<AbhaAccount>> SearchAbhaAsync(string mobile)
        {
            var config = await GetAbdmConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.searchAbhaUrl))
                throw new AbdmConfigMissingException("ABDM configuration missing or invalid.");

            var token = await _authService.GetAccessTokenAsync();
            var publicKey = await _authService.GetPublicKeyAsync();
            var encryptedMobile = Encryptor.EncryptWithPublicKeyString(mobile, publicKey);

            HttpRequestHeaderHelper.ApplyDefaultHeaders(_httpClient, token, true, true);

            var body = new { scope = new[] { "search-abha" }, mobile = encryptedMobile };
            var response = await _httpClient.PostAsJsonAsync(config.searchAbhaUrl, body);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (json.Contains("ABDM-1114"))
                    throw new AbdmUserNotFoundException($"No ABHA account found for mobile: {mobile}");
                if (json.Contains("ABDM-1115"))
                    throw new InvalidOtpException("Invalid OTP entered.");
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    throw new TooManyRequest("Too many requests — please try again later.");

                throw new AbdmException($"ABDM search failed: {json}");
            }

            return JsonSerializer.Deserialize<List<AbhaAccount>>(json) ?? new List<AbhaAccount>();
        }
    
    public async Task<OtpResponse> RequestOtpLoginAsync(int index, string TxnId)
        {
            try
            {
                var config = await GetAbdmConfigAsync();
                var token = await _authService.GetAccessTokenAsync();
                var publicKey = await _authService.GetPublicKeyAsync();

                var encryptedIndex = Encryptor.EncryptWithPublicKeyString(index.ToString(), publicKey);

                HttpRequestHeaderHelper.ApplyDefaultHeaders(_httpClient, token, includeRequestId: true, includeTimestamp: true);
                _httpClient.DefaultRequestHeaders.Add("BENEFIT_NAME", "benefit-name");

                var body = new
                {
                    scope = new[] { "abha-login", "search-abha", "mobile-verify" },
                    loginHint = "index",
                    loginId = encryptedIndex,
                    otpSystem = "abdm",
                    txnId = TxnId
                };

                var response = await _httpClient.PostAsJsonAsync(config.abhaLoginOTPRequestUrl, body);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"ABDM OTP request failed: {json}");

                return JsonSerializer.Deserialize<OtpResponse>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in RequestOtpSearchAsync: {ex.Message}", ex);
            }
        }

        public async Task<VerifyOtpResponse> VerifyAbhaLoginAsync(string txnId, string otp)
        {
            try
            {
                var config = await GetAbdmConfigAsync();
                var token = await _authService.GetAccessTokenAsync();
                var publicKey = await _authService.GetPublicKeyAsync();

                var encryptedOtp = Encryptor.EncryptWithPublicKeyString(otp, publicKey);
                HttpRequestHeaderHelper.ApplyDefaultHeaders(_httpClient, token, includeRequestId: true, includeTimestamp: true);

                var body = new
                {
                    scope = new[] { "abha-login", "mobile-verify" },
                    authData = new
                    {
                        authMethods = new[] { "otp" },
                        otp = new
                        {
                            txnId = txnId,
                            otpValue = encryptedOtp
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(config.abhaLoginUrl, body);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"ABDM Verify OTP failed: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var result = new VerifyOtpResponse { TxnId = txnId, Accounts = new List<AbhaAccountDTO>() };

                if (root.TryGetProperty("authResult", out var authResult) && authResult.GetString() == "failed")
                {
                    var msg = root.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : "Invalid OTP";
                    throw new InvalidOtpException(msg);
                }

                if (root.TryGetProperty("accounts", out var accounts))
                {
                    foreach (var acc in accounts.EnumerateArray())
                    {
                        result.Accounts.Add(new AbhaAccountDTO
                        {
                            ABHANumber = acc.GetProperty("ABHANumber").GetString(),
                            PreferredAbhaAddress = acc.GetProperty("preferredAbhaAddress").GetString(),
                            Name = acc.GetProperty("name").GetString()
                        });
                    }
                }

                return result;
            }
            catch (InvalidOtpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in VerifyAbhaSearchAsync: {ex.Message}", ex);
            }
        }

        // Searches patients in Danphe EMR database by mobile number
        public async Task<List<PatientSerachDTO>> SearchPatientByMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return null;

            var patient = await _DbContext.Patient
                .Where(p => p.PhoneNumber == mobile)
                .Select(p => new PatientSerachDTO
                {
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    AbhaNumber = p.EHRNumber,
                    AbhaAddress = null
                })
                .ToListAsync();

            return patient;
        }
    }
}
