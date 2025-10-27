using Asp.netWebAPP.Core.Application.DTO_s;
using Asp.netWebAPP.Core.Domain.Model;

namespace Asp.netWebAPP.Infrastructure.Data.FHIR_Linq
{
    public class FhirPrescriptionQueries
    {
        private readonly DanpheDbContext _context;

        public FhirPrescriptionQueries(DanpheDbContext context)
        {
            _context = context;
        }

       
        public PatientFHIRDto GetPatient(int patientId)
        {
            var PatientData= (from p in _context.Patient
                    join csd in _context.CountrySubDivision on p.CountrySubDivisionId equals csd.CountrySubDivisionId into csdGroup
                    from csd in csdGroup.DefaultIfEmpty()
                    join c in _context.Country on p.CountryId equals c.CountryId into cGroup
                    from c in cGroup.DefaultIfEmpty()
                    where p.PatientId == patientId
                    select new PatientFHIRDto
                    {
                        PatientId = p.PatientId,
                        FullName = p.ShortName,
                        Gender = p.Gender,
                        BirthDate = p.DateOfBirth,
                        Phone = p.PhoneNumber,
                        Address = p.Address,
                        Country = c.CountryName,
                        CountryCode = c.CountryShortName,
                        State = csd.CountrySubDivisionName
                    }).FirstOrDefault();
            return PatientData;
        }

    
        public PractitionerFHIRDto GetPractitioner(int employeeId)
        {
            var PractitioneData=(from emp in _context.Employees
                    where emp.EmployeeId == employeeId
                    select new PractitionerFHIRDto
                    {
                        PractitionerId = emp.EmployeeId,
                        FullName = emp.FullName,
                        Gender = emp.Gender,
                        BirthDate = emp.DateOfBirth ?? new DateTime(1980, 06, 10),
                        ContactNumber = string.IsNullOrEmpty(emp.ContactNumber) ? "0000000000" : emp.ContactNumber,
                        Address = string.IsNullOrEmpty(emp.ContactAddress) ? "Address not available" : emp.ContactAddress,
                        LicenseNumber = emp.MedCertificationNo ?? "TEMP-DOCTOR-ID-001",
                        //Qualification = emp.Qualification ?? "MBBS"
                    }).FirstOrDefault();
            return PractitioneData;
        }

     
        public PrescriptionCompositionFHIRDto GetPrescriptionComposition(int prescriptionId)
        {
            var CompositionData= (from pres in _context.PHRMPrescription
                    join pat in _context.Patient on pres.PatientId equals pat.PatientId
                    join emp in _context.Employees on pres.PrescriberId equals emp.EmployeeId
                    where pres.PrescriptionId == prescriptionId
                    select new PrescriptionCompositionFHIRDto
                    {
                        PrescriptionId = pres.PrescriptionId,
                        PatientId = pat.PatientId,
                        PractitionerId = emp.EmployeeId,
                        CreatedOn = pres.CreatedOn,
                        MedicationItemIds = (from presItm in _context.PHRMPrescriptionItems
                                             where presItm.PrescriptionId == pres.PrescriptionId
                                             select presItm.ItemId ?? 0).ToList()
                    }).FirstOrDefault();
            return CompositionData;
        }

      
        public List<MedicationRequestFHIRDto> GetMedicationRequestsByVisitId(int patientVisitId)
        {
            var MedicationRequestData= (from pres in _context.PHRMPrescription
                    join pat in _context.Patient on pres.PatientId equals pat.PatientId
                    join emp in _context.Employees on pres.PrescriberId equals emp.EmployeeId
                    join presItm in _context.PHRMPrescriptionItems on pres.PrescriptionId equals presItm.PrescriptionId
                    join itm in _context.PHRMItemMaster on presItm.ItemId equals itm.ItemId
                    where pres.PatientVisitId == patientVisitId
                    select new MedicationRequestFHIRDto
                    {
                        PrescriptionId = pres.PrescriptionId,
                        PatientId = pat.PatientId,
                        PractitionerId = emp.EmployeeId,
                        MedicationName = itm.ItemName,
                        DosageText = presItm.Dosage,
                        Frequency = presItm.Frequency ?? 1,
                        Period = 1,
                        RouteDisplay = presItm.Route ?? "Oral route",
                        AuthoredOn = pres.CreatedOn,
                        PrescribedDate = pres.CreatedOn
                    }).ToList();
            return MedicationRequestData;
        }
    }
}
