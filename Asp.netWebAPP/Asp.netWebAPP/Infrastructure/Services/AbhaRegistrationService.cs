using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.Interface;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Asp.netWebAPP.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Asp.netWebAPP.Infrastructure.Data;
using Asp.netWebAPP.Core.Domain.Value_Objects;
using Asp.netWebAPP.Core.Shared.Exceptions;

namespace Asp.netWebAPP.Infrastructure.Services
{
    public class AbhaRegistrationService : IAbhaRegistrationService
    {
        private readonly HttpClient _httpClient;
        private readonly AbdmDbContext _dbContext;
        private readonly IAbhaAuthService _authService;
        public AbhaRegistrationService(
            AbdmDbContext dbContext,
            IAbhaAuthService authService,
            HttpClient httpClient,
                                       IAbhaLoginService loginService)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _authService = authService;
        }
        // Fetches ABDM configuration details from the database
        private async Task<AbdmConfigDTO> GetAbdmConfigAsync()
        {
            var row = await _dbContext.AbdmCore_Parameters
                .FirstOrDefaultAsync(p => p.ParameterGroupName == "ABDM");

            if (row == null)
                throw new Exception("ABDM_Config row not found in Core_Parameters");

            return JsonSerializer.Deserialize<AbdmConfigDTO>(row.ParameterValue)
                   ?? throw new Exception("Failed to deserialize ABDM config.");
        }   
        public async Task<OtpResponse> RequestRegisterOtpAsync(string aadhaarNumber)
        {
            try
            {
                var config = await GetAbdmConfigAsync();
                var accessToken = await _authService.GetAccessTokenAsync();
                var publicKey = await _authService.GetPublicKeyAsync();
                var encryptedAadhaar = Encryptor.EncryptWithPublicKeyString(aadhaarNumber, publicKey);
                HttpRequestHeaderHelper.ApplyDefaultHeaders(_httpClient, accessToken, true, true);

                var payload = new
                {
                    txnId = "",
                    scope = new[] { "abha-enrol" },
                    loginHint = "aadhaar",
                    loginId = encryptedAadhaar,
                    otpSystem = "aadhaar"
                };
                var response = await _httpClient.PostAsJsonAsync(config.abhaOTPrequestUrl, payload);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (json.Contains("\"loginId\":\"Invalid LoginId\""))
                        throw new InvalidAadhaarException("Invalid Aadhaar number.");

                    if (json.Contains("ABDM-1204"))
                        throw new TooManyRequest("Too many OTP requests. Please wait and try again.");

                    throw new Exception($"ABDM server error: {json}");
                }

                var otpResponse = JsonSerializer.Deserialize<OtpResponse>(json);
                if (otpResponse == null || string.IsNullOrEmpty(otpResponse.txnId))
                    throw new InvalidAadhaarException("Failed to generate OTP. Please try again.");

                return otpResponse;
            }
            catch (InvalidAadhaarException)
            {
                throw; 
            }
            catch (TooManyRequest)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in RequestRegisterOtpAsync: {ex.Message}", ex);
            }
        }
        // Verifies OTP for ABHA registration and creates ABHA profile
        public async Task<VerifyRegisterOtpResponse> VerifyAbhaRegistrationAsync(
      string txnId,
      string otp,
      string mobile)
        {
            if (string.IsNullOrWhiteSpace(txnId) ||
                string.IsNullOrWhiteSpace(otp) ||
                string.IsNullOrWhiteSpace(mobile))
                throw new ApplicationException("TxnId, OTP, and Mobile are required.");
            var config = await GetAbdmConfigAsync();
            var accessToken = await _authService.GetAccessTokenAsync();
            var publicKey = await _authService.GetPublicKeyAsync();
            var encryptedOtp = Encryptor.EncryptWithPublicKeyString(otp, publicKey);
            var requestBody = new
            {
                authData = new
                {
                    authMethods = new[] { "otp" },
                    otp = new
                    {
                        txnId,
                        otpValue = encryptedOtp,
                        mobile
                    }
                },
                consent = new
                {
                    code = "abha-enrollment",
                    version = "1.4"
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            HttpRequestHeaderHelper.ApplyDefaultHeaders(
                _httpClient,
                accessToken,
                includeRequestId: true,
                includeTimestamp: true
            );

            var response = await _httpClient.PostAsync(config.abhaCreationUrl, requestContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (responseString.Contains("ABDM-1204"))
                    throw new InvalidOtpException("Invalid OTP. Please try again.");

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    throw new TooManyRequest("Too many OTP attempts. Please wait and try again.");

                throw new ApplicationException($"ABHA API returned {response.StatusCode}: {responseString}");
            }
            var result = JsonSerializer.Deserialize<VerifyRegisterOtpResponse>(
                responseString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            if (result?.ABHAProfile != null)
            {
                result.ABHAProfile.Mobile = mobile;
                result.ABHAProfile.AbhaNumber = result.ABHAProfile.PreferredAddress?.Split('@')[0];
            }

            return result;
        }
        
    }
}
