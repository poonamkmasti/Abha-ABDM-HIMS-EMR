using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Application.M2.Commands;
using Asp.netWebAPP.Core.Domain.Model;
using System.Text.Json;

namespace Asp.netWebAPP.Core.Application.Interface
{
    public interface IBridgeService
    {
        Task<RegisterBridgeServiceResponseDTO> RegisterBridgeServiceAsync(RegisterBridgeServiceCommand command);
        Task<bool> SaveCallbackAsync(JsonElement payload, string callbackUrl);

    }

}
