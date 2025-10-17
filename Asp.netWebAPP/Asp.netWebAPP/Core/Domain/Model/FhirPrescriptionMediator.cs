using Asp.netWebAPP.Core.Application.DTO_s;

namespace Asp.netWebAPP.Core.Domain.Model
{
    public class FhirPrescriptionMediator
    {
        public PatientFHIRDto Patient { get; set; }
        public PractitionerFHIRDto Practitioner { get; set; }
        public PrescriptionCompositionFHIRDto Composition { get; set; }
        public List<MedicationRequestFHIRDto> Medications { get; set; }
    }


}
