using System.ComponentModel.DataAnnotations;

namespace Asp.netWebAPP.Core.Domain.Model
{
    public class AbdmCallbackLog
    {

        [Key]

        public int CallbackJsonId { get; set; }

        public string AbhaAddress { get; set; } = string.Empty;

        public string CallbackUrl { get; set; } = string.Empty;

        public string RawJsonPayload { get; set; } = string.Empty;

        public DateTime ReceivedOn { get; set; } = DateTime.UtcNow;

    }
}
