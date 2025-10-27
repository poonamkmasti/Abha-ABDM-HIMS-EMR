namespace Asp.netWebAPP.Core.Domain.Model
{
    public class PatientFHIRDto
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
    }
}
