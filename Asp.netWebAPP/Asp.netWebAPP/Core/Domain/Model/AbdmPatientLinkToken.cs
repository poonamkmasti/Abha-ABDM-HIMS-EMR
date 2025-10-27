using System.ComponentModel.DataAnnotations;

namespace Asp.netWebAPP.Core.Domain.Model
{
    public class AbdmPatientLinkToken
    {
        [Key]
        public int LinkTokenId { get; set; }  

        public string AbhaAddress { get; set; } = string.Empty;  

        public string LinkToken { get; set; } = string.Empty;  

        public DateTime LinkTokenExpiry { get; set; }  

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;  
    }
}
