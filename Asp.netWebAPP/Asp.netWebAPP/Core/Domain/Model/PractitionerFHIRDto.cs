namespace Asp.netWebAPP.Core.Domain.Model
{
    public class PractitionerFHIRDto
    {

        public int PractitionerId { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
       // public string Qualification { get; set; }
        public string LicenseNumber { get; set; }
        public string CountryCode { get; set; }
        public string TelecomValue { get; set; }

        //  Identifier (FHIR)
        public string IdentifierUse { get; set; } = "official";
        public string IdentifierSystem { get; set; } = "http://terminology.hl7.org/CodeSystem/v2-0203";
        public string LicenseSystem { get; set; } = "https://doctor.ndhm.gov.in";
        public string LicenseTypeCode { get; set; } = "MD";  // MedCertificationNo
        public string LicenseTypeDisplay { get; set; } = "Medical License number";
        public string AssignerDisplay { get; set; } = "Medical Council of India";

        //  Telecom
        public string TelecomSystem { get; set; } = "phone";
        public string TelecomUse { get; set; } = "work";

        //  Address
        public string Country { get; set; } = "IN";

        //  Qualification 
        public string QualificationCode { get; set; } = "76231001";  // SNOMED: Doctor of Medicine
        public string QualificationDisplay { get; set; } = "Doctor of Medicine";
        public string QualificationText { get; set; } = "MD";
        public string QualificationIssuer { get; set; } = "Delhi Medical Council";
    }
}
