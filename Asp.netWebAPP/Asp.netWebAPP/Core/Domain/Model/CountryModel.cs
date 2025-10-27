using System.ComponentModel.DataAnnotations;

namespace Asp.netWebAPP.Core.Domain.Model
{
    public class CountryModel
    {
        [Key]
        public int CountryId { get; set; }
        public string CountryShortName { get; set; }
        public string CountryName { get; set; }
        public string ISDCode { get; set; }
        public string CountrySubDivisionType { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? MobileMinDigits { get; set; }
        public int? MobileMaxDigits { get; set; }

        public string MobileAllowedStartDigits { get; set; }
        public int? TelephoneMinDigits { get; set; }
        public int? TelephoneMaxDigits { get; set; }
        public string TelephoneAllowedStartDigits { get; set; }
    }
}
