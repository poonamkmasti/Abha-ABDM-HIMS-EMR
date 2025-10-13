using Asp.netWebAPP.Core.Application.ABHA.Commands;
using Asp.netWebAPP.Core.Application.ABHA.Commands.Handlers;
using Asp.netWebAPP.Core.Application.ABHA.Queries.Handler;
using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Asp.netWebAPP.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbhaRegisterController : ControllerBase
    {
        private readonly RequestRegisterOtpHandler _requestRegisterOtpHandler;
        private readonly VerifyRegisterOtpHandler _verifyRegisterOtpHandler;

        public AbhaRegisterController(
            RequestRegisterOtpHandler requestRegisterOtpHandler,
            VerifyRegisterOtpHandler verifyRegisterOtpHandler
        )
        {
            _requestRegisterOtpHandler = requestRegisterOtpHandler;
            _verifyRegisterOtpHandler = verifyRegisterOtpHandler;
        }

        [HttpPost("request-otp-register")]
        public async Task<ActionResult<OtpResponse>> RequestOtpRegister([FromBody] RequestRegisterOtpCommand command)
        {
            if (string.IsNullOrEmpty(command.AadhaarNumber))
                return BadRequest(new { message = "Aadhaar number is required." });

            try
            {
                var result = await _requestRegisterOtpHandler.Handle(command);
                return Ok(result);
            }
            catch (InvalidAadhaarException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (TooManyRequest ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
            catch (AbdmConfigMissingException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An unexpected error occurred.",
                    details = ex.Message
                });
            }
        }

        [HttpPost("verify-otp-register")]
        public async Task<ActionResult<VerifyRegisterOtpResponse>> VerifyOtpRegister([FromBody] VerifyRegisterOtpCommand command)
        {
            try
            {
                if (string.IsNullOrEmpty(command.TxnId) || string.IsNullOrEmpty(command.Otp) || string.IsNullOrEmpty(command.Mobile))
                    return BadRequest(new { message = "TxnId, OTP, and Mobile are required." });

                var result = await _verifyRegisterOtpHandler.Handle(command);
                return Ok(result);
            }
            catch (InvalidOtpException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (AbdmConfigMissingException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (TooManyRequest ex)
            {
                return StatusCode(429, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
