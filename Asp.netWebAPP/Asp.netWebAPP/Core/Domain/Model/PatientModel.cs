using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Asp.netWebAPP.Core.Domain.Model
{
    [Table("PAT_Patient")] 
    public class PatientModel
    {
        [Key]
        public int PatientId { get; set; } 

        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EHRNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? EncryptedLinkToken { get; set; }  
        public DateTime? LinkTokenExpiry { get; set; } 
        public DateTime? CreatedOn { get; set; }
        public int? CountrySubDivisionId { get; set; }
        public int? CountryId { get; set; }
        public string ShortName { get; set; }
        public string Address { get; set; }


    }
}
