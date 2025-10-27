using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.M2.Commands;
using Asp.netWebAPP.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asp.netWebAPP.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CareContextLinkController : Controller
    {
        private readonly IMediator _mediator;

        private readonly DanpheDbContext _DanpheDbContext;

        public CareContextLinkController(IMediator mediator,DanpheDbContext danpheDbContext)
        {
            _mediator = mediator;
            _DanpheDbContext = danpheDbContext;
        }

        [HttpPost("linkcarecontext")]
        public async Task<IActionResult> LinkCareContext([FromBody] CareContextLinkRequestDTO request)
        {
            var result = await _mediator.Send(new LinkCareContextCommand  { Request = request });
            return Ok(result);
        }

    }
}
