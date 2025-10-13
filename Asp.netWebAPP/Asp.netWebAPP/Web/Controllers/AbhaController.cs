using Microsoft.AspNetCore.Mvc;
using Asp.netWebAPP.Core.Application.ABHA.Commands.Handlers;
using Asp.netWebAPP.Core.Application.ABHA.Commands;
using Asp.netWebAPP.Core.Application.ABHA.Queries.Handler;
using Asp.netWebAPP.Core.Application.ABHA.Queries;
using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Domain.Model;
using Asp.netWebAPP.Core.Shared.Exceptions;

namespace Backend.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbhaController : ControllerBase
    {
        private readonly SearchAbhaHandler _searchHandler;
        private readonly RequestOtpLoginHandler _otpHandler;
        private readonly VerifyOtpHandler _verifyHandler;
        private readonly SearchPatientByMobileHandler _searchPatientHandler;

        public AbhaController(
            SearchAbhaHandler searchHandler,
            RequestOtpLoginHandler otpHandler,
            VerifyOtpHandler verifyHandler,
            SearchPatientByMobileHandler patientHandler)
        {
            _searchHandler = searchHandler;
            _otpHandler = otpHandler;
            _verifyHandler = verifyHandler;
            _searchPatientHandler = patientHandler;
        }

        [HttpPost("search-abha")]
        public async Task<ActionResult<List<AbhaAccount>>> SearchAbha([FromBody] SearchAbhaRequest request)
        {
            if (string.IsNullOrEmpty(request.Mobile))
                return BadRequest(new { type = "ValidationError", message = "Mobile number is required." });

            try
            {
                var result = await _searchHandler.Handle(new SearchAbhaQuery(request.Mobile));
                return Ok(result);
            }
            catch (AbdmUserNotFoundException ex)
            {
                return NotFound(new { type = "NotFound", message = ex.Message });
            }
            catch (InvalidOtpException ex)
            {
                return BadRequest(new { type = "InvalidOtp", message = ex.Message });
            }
            catch (TooManyRequest ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { type = "TooManyRequests", message = ex.Message });
            }
            catch (AbdmConfigMissingException ex)
            {
                return BadRequest(new { type = "ConfigMissing", message = ex.Message });
            }
            catch (AbdmException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { type = "AbdmError", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { type = "ServerError", message = ex.Message });
            }
        }
        
        [HttpPost("request-otp-login")]
        public async Task<ActionResult<OtpResponse>> RequestOtpLogin([FromBody] RequestOtpLoginCommand command)
        {
            try
            {
                var result = await _otpHandler.Handle(command);
                return Ok(result);
            }
            catch (TooManyRequest ex)
            {
                return BadRequest(new { type = "TooManyRequest", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    type = "ServerError",
                    message = ex.Message
                });
            }
        }

        [HttpPost("verify-abha-login")]
        public async Task<ActionResult<VerifyOtpResponse>> VerifyAbhaLogin([FromBody] VerifyOtpCommand command)
        {
            try
            {
                var result = await _verifyHandler.Handle(command);
                if (result == null || (result.Accounts != null && !result.Accounts.Any() && !string.IsNullOrEmpty(result.Message)))
                {
                    return BadRequest(new { type = "InvalidOtp", message = result.Message ?? "Invalid OTP entered." });
                }

                return Ok(result);
            }
            catch (InvalidOtpException ex)
            {
                return BadRequest(new { type = "InvalidOtp", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    type = "ServerError",
                    message = ex.Message
                });
            }
        }

        [HttpPost("search-patient")]
        public async Task<ActionResult<List<PatientSerachDTO>>> SearchPatient([FromBody] SearchAbhaRequest request)
        {
            if (string.IsNullOrEmpty(request.Mobile))
                return BadRequest(new { type = "ValidationError", message = "Mobile number is required." });

            try
            {
                var result = await _searchPatientHandler.Handle(new SearchPatientByMobileQuery(request.Mobile));

                if (result == null || !result.Any())
                    return NotFound(new { type = "NotFound", message = "No patient found for the given mobile number in Danphe." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    type = "ServerError",
                    message = ex.Message
                });
            }
        }
    }
}
