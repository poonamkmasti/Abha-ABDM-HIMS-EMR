namespace Asp.netWebAPP.Core.Domain.Model
{
    public class MedicationRequestFHIRDto
    {
        public int PrescriptionId { get; set; }
        public string MedicationCode { get; set; }
        public string MedicationName { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int PractitionerId { get; set; }
        public int PatientVisitId { get; set; }
        public string PractitionerName { get; set; }
        public DateTime? PrescribedDate { get; set; }
        public string DosageText { get; set; }
        public int Frequency { get; set; }
        public decimal? Period { get; set; }
        public string Route { get; set; }
        public DateTime? AuthoredOn { get; set; }
        public string Status { get; set; } = "active";
        public string Intent { get; set; } = "order";
        public string FrequencySystem { get; set; }
        public string FrequencyCode { get; set; }
        public string FrequencyDisplay { get; set; }
        public string RouteSystem { get; set; }
        public string RouteCode { get; set; }
        public string RouteDisplay { get; set; }
        public string MethodSystem { get; set; }
        public string MethodCode { get; set; }
        public string MethodDisplay { get; set; }
    }
}
