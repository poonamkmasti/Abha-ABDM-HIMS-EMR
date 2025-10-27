using Asp.netWebAPP.Core.Application.Interface;
using Asp.netWebAPP.Core.Application.M2.Commands;
using Asp.netWebAPP.Core.Domain.Model;
using Asp.netWebAPP.Infrastructure.Services;
using MediatR;

namespace Asp.netWebAPP.Core.Application.M2.Handlers
{
    public class StoreAbdmCallbackHandler : IRequestHandler<StoreAbdmCallbackCommand, bool>
    {
        private readonly IBridgeService _bridgeService;

        public StoreAbdmCallbackHandler(IBridgeService bridgeService)
        {
            _bridgeService = bridgeService;
        }

        public async Task<bool> Handle(StoreAbdmCallbackCommand request, CancellationToken cancellationToken)
        {
            // Delegate all logic to service layer
            return await _bridgeService.SaveCallbackAsync(request.Payload, request.CallbackUrl);
        }
    }
}
