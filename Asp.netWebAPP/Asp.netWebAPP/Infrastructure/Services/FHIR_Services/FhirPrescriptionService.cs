using Asp.netWebAPP.Core.Domain.Model;
using Asp.netWebAPP.Infrastructure.Data.FHIR_Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace ABDM.FHIR.Services
{
    public class FhirPrescriptionService
    {
        private readonly ResourcePopulatorService _resourceService;
        private readonly FhirPrescriptionQueries _queries;

        public FhirPrescriptionService(ResourcePopulatorService resourceService,
                                       FhirPrescriptionQueries queries)
        {
            _resourceService = resourceService;
            _queries = queries;
        }

        public FhirPrescriptionMediator GetFhirPrescriptionData(int patientId, int practitionerId, int prescriptionId, int patientVisitId)
        {
            // Fetch data using your LINQ methods
            var patientData = _queries.GetPatient(patientId);
            var practitionerData = _queries.GetPractitioner(practitionerId);
            var compositionData = _queries.GetPrescriptionComposition(prescriptionId);
            var medicationList = _queries.GetMedicationRequestsByVisitId(patientVisitId);

            // Build and return mediator object
            var mediator = new FhirPrescriptionMediator
            {
                Patient = patientData,
                Practitioner = practitionerData,
                Composition = compositionData,
                Medications = medicationList
            };

            return mediator;
        }



        // Public method to generate the FHIR Prescription Bundle as JSON
        public string GeneratePrescriptionBundle(int patientId, int practitionerId, int prescriptionId, int patientVisitId)
        {
            try
            {
                var mediator = GetFhirPrescriptionData(patientId, practitionerId, prescriptionId, patientVisitId);


                // Step 1: Populate the bundle using ResourcePopulator
                Bundle prescriptionBundle = populatePrescriptionBundle(mediator);

                // Step 2: Validate bundle against ABDM FHIR profiles
                string validationError = "";
                bool isValid = _resourceService.ValidateProfile(prescriptionBundle, ref validationError);
                if (!isValid)
                {
                    // You can log validation errors here
                    Console.WriteLine("Validation Errors: " + validationError);
                }

                // Step 3: Serialize bundle to JSON
                var json = new FhirJsonSerializer().SerializeToString(prescriptionBundle);
                return json;
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine("Error generating prescription bundle: " + ex.Message);
                return null;
            }
        }

        // Internal method to populate the bundle
        private Bundle populatePrescriptionBundle(FhirPrescriptionMediator mediator)
        {
            // Set metadata about the resource
            Bundle prescriptionBundle = new Bundle()
            {
                Id = $"Prescription-{mediator.Composition.PrescriptionId}",
                Meta = new Meta()
                {
                    VersionId = "1",
                    LastUpdatedElement = new Instant(DateTimeOffset.Now),
                    Profile = new List<string>()
                    {
                        "https://nrces.in/ndhm/fhir/r4/StructureDefinition/DocumentBundle",
                    },
                    Security = new List<Coding>()
                    {
                        new Coding("http://terminology.hl7.org/CodeSystem/v3-Confidentiality", "V", "very restricted"),
                    }
                },
                Identifier = new Identifier
                {
                    ElementId = "BundleID",
                    Value = Guid.NewGuid().ToString(),
                    System = "http://hip.in"
                },
                Type = Bundle.BundleType.Document,
                TimestampElement = new Instant(DateTimeOffset.Now)
            };

            // Add entries using ResourcePopulator
            prescriptionBundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:composition-{mediator.Composition.PrescriptionId}",
                Resource = _resourceService.populatePrescriptionCompositionResource(mediator.Composition)
            });

            prescriptionBundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:patient-{mediator.Patient.PatientId}",
                Resource = _resourceService.populatePatientResource(mediator.Patient)
            });

            prescriptionBundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:practitioner-{mediator.Practitioner.PractitionerId}",
                Resource = _resourceService.populatePractitionerResource(mediator.Practitioner)
            });

            // ✅ Add all Medications
            foreach (var med in mediator.Medications)
            {
                // Generate a safe and unique identifier for this medication entry
                string safeId = $"medRequest-{med.PrescriptionId}-{med.PatientId}-{Guid.NewGuid()}";

                prescriptionBundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{safeId}",
                    Resource = _resourceService.populateMedicationRequestResource(med, safeId)
                });

            }


            //prescriptionBundle.Entry.Add(new Bundle.EntryComponent
            //{
            //    FullUrl = "urn:uuid:medicationrequest-02",
            //    Resource = _resourceService.populateSecondMedicationRequestResource()
            //});



            prescriptionBundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = "urn:uuid:binary-01",
                Resource = _resourceService.populateBinaryResource()
            });

            // Add signature
            prescriptionBundle.Signature = _resourceService.populateSignature();

            return prescriptionBundle;
        }
    }
}
