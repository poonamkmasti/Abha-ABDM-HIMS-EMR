using Asp.netWebAPP.Core.Application.M2.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Asp.netWebAPP.Web.Controllers
{
    [ApiController]
    [Route("api/v3")]
    public class BridgeController : Controller
    {
        private readonly IMediator _mediator;

        private readonly ILogger<BridgeController> _logger;



        public BridgeController(ILogger<BridgeController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;

        }
        [HttpPost("register-service")]
        public async Task<IActionResult> RegisterService([FromBody] RegisterBridgeServiceCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        [HttpPost("{**catchAll}")]
        public async Task<IActionResult> ReceiveCallback([FromBody] JsonElement payload)
        {
            string callbackUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            _logger.LogInformation("📩 Callback received at {CallbackUrl}", callbackUrl);

            var command = new StoreAbdmCallbackCommand
            {
                Payload = payload,
                CallbackUrl = callbackUrl
            };

            var result = await _mediator.Send(command);

            if (result)
                return Ok(new { success = true });
            else
                return StatusCode(500, new { success = false, message = "Failed to store callback." });
        }
       
    }
}
