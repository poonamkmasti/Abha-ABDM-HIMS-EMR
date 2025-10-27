using MediatR;
using System.Text.Json;

namespace Asp.netWebAPP.Core.Application.M2.Commands
{
    //public class StoreAbdmCallbackCommand : IRequest<bool>
    //{
    //    public JsonElement Payload { get; set; }
    //}
    public class StoreAbdmCallbackCommand : IRequest<bool>
    {
        public JsonElement Payload { get; set; }
        public string CallbackUrl { get; set; } = string.Empty; // new property for URL
    }
}
