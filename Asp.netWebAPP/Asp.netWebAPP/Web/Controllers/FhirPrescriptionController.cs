using ABDM.FHIR.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Asp.netWebAPP.Web.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class FhirPrescriptionController : ControllerBase
    {
        private readonly FhirPrescriptionService _prescriptionService;

        public FhirPrescriptionController(FhirPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        [HttpGet("bundle")]
        public IActionResult GetPrescriptionBundle([FromQuery] int patientId, [FromQuery] int practitionerId, [FromQuery] int prescriptionId, [FromQuery] int patientVisitId)
        {
            try
            {
                string jsonBundle = _prescriptionService.GeneratePrescriptionBundle(patientId, practitionerId, prescriptionId, patientVisitId);
                if (string.IsNullOrEmpty(jsonBundle))
                {
                    return StatusCode(500, "Failed to generate prescription bundle.");
                }

                // Return as JSON
                return Content(jsonBundle, "application/fhir+json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        

    }

}
