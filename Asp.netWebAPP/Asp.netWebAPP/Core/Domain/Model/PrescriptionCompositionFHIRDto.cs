namespace Asp.netWebAPP.Core.Domain.Model
{
    public class PrescriptionCompositionFHIRDto
    {
        public int PrescriptionId { get; set; }
        public string CompositionStatus { get; set; } = "final";
        public string CompositionTypeCode { get; set; } = "440545006";
        public string CompositionTypeSystem { get; set; } = "http://snomed.info/sct";
        public string CompositionTypeDisplay { get; set; } = "Prescription record";
        public string CompositionTitle { get; set; } = "Prescription record";


        public int PatientId { get; set; }                      // → subject.reference
        public string PatientName { get; set; }                 // → subject.display

        public int PractitionerId { get; set; }                 // → author.reference
        public string PractitionerName { get; set; }            // → author.display


        public DateTime? CreatedOn { get; set; }

        // FHIR Section Entries (MedicationRequest references)
        public List<int> MedicationItemIds { get; set; } = new List<int>();

        // Optional Metadata
        public string IdentifierSystem { get; set; } = "https://ndhm.in/phr";
        public string MetaProfile { get; set; } = "https://nrces.in/ndhm/fhir/r4/StructureDefinition/PrescriptionRecord";
    }
}
