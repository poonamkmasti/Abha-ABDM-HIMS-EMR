using Asp.netWebAPP.Core.Domain.Model;
//using Asp.netWebAPP.Infrastructure.Services.FHIR_Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Net;
using System.Text;

namespace ABDM.FHIR.Services
{
    public class ResourcePopulatorService
    {
        // If you need logging or configuration
        private readonly ILogger<ResourcePopulatorService> _logger;
      

        public ResourcePopulatorService(ILogger<ResourcePopulatorService> logger)
        {
            _logger = logger;

        }

       

        // Example: Populate Patient Resource
        public Patient PopulatePatientResource()
        {
            Patient patient = new Patient()
            {
                Meta = new Meta()
                {
                    Profile = new List<string>()
                {
                    "https://nrces.in/ndhm/fhir/r4/StructureDefinition/Patient",
                },
                    VersionId = "1",
                    LastUpdatedElement = new Instant(new DateTimeOffset(2020, 07, 09, 15, 32, 26, new TimeSpan(1, 0, 0)))
                },
            };

            patient.Id = "23986a85-fb64-48c2-ab85-3462586cc134";

            var id = new Identifier();
            id.System = "https://ndhm.in/SwasthID";
            id.Value = "1234";
            id.Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "MR", "Medical record number", "Text");
            patient.Identifier.Add(id);

            patient.Name.Add(new HumanName { Text = "ABC" });
            patient.Gender = AdministrativeGender.Female;
            patient.BirthDate = "1981-01-12";

            patient.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = "+919818512600",
                Use = ContactPoint.ContactPointUse.Home
            });

            return patient;
        }

        // Add all other methods here, remove 'static' keyword
        // e.g., PopulatePractitionerResource(), PopulateMedicationRequestResource(), PopulateConditionResource(), etc.

        public bool seralize_WriteFile(string filename_IN, Base profile_IN)
        {
            bool isSuccess = true;
            bool inValidFileExtension = false;
            try
            {

                FhirJsonSerializer serializer = new FhirJsonSerializer(new SerializerSettings()
                {
                    Pretty = true,
                });

                FhirXmlSerializer serializerXML = new FhirXmlSerializer(new SerializerSettings()
                {
                    Pretty = true,
                });

                Console.WriteLine("\nEnter file path to write bundle (extension .json or .xml):");
                filename_IN = Console.ReadLine();
                string filepath = filename_IN;
                FileInfo fi = new FileInfo(filepath);

                // Serialize populated bundle to Json 
                if (fi.Extension == ".json")
                {
                    string bundeljson = serializer.SerializeToString(profile_IN);
                    File.WriteAllText(filepath, bundeljson);

                }
                else if (fi.Extension == ".xml")
                {
                    string bundelxml = serializerXML.SerializeToString(profile_IN);
                    File.WriteAllText(filepath, bundelxml);
                }
                else
                {
                    Console.WriteLine("Invalid file extension!");
                    isSuccess = false;
                    inValidFileExtension = true;
                }

                if (inValidFileExtension != true)
                {
                    // Parse the xml/json file
                    Base profile = null;
                    if (fi.Extension == ".json")
                    {
                        var parser = new FhirJsonParser();
                        profile = parser.Parse(File.ReadAllText(filepath));
                    }
                    else if (fi.Extension == ".xml")
                    {
                        var parser = new FhirXmlParser();
                        profile = parser.Parse(File.ReadAllText(filepath));
                    }
                    else
                    {
                        Console.WriteLine("Invalid file extension!");
                        isSuccess = false;
                    }

                    // Validate Parsed file
                    string strErr_OUT = "";
                    if (ValidateProfile(profile, ref strErr_OUT) == true)
                    {
                        Console.WriteLine("Validated parsed file successfully");
                        isSuccess = true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to validate parsed file");
                        isSuccess = false;
                    }
                }
                return isSuccess;
            }
            catch (Exception e)
            {
                Console.WriteLine("seralize_WriteFile ERROR:-" + e.Message);
                isSuccess = false;
                return isSuccess;
            }

        }
        //This method validates the FHIR resources 
        // Param      
        public bool ValidateProfile(Base ProfileInstance, ref string strError_OUT)
        {
            bool isValid = true;
            string JsonFileName = "";
            try
            {
                #region Validation
                // The URL profile shall be mentioned below
                string profileURL = "https://nrces.in/ndhm/fhir/r4/package.tgz";
                if (fnDownloadPackage(profileURL) == true)
                {
                    // The path of the extracted profile directory shall be mentioned below
                    string parentDirName = new FileInfo(AppDomain.CurrentDomain.BaseDirectory).Directory.Parent.FullName;
                    string path = parentDirName + "\\Debug\\package\\";
                    string profiledirectory = path;

                    IResourceResolver resolver = new CachedResolver(new MultiResolver(ZipSource.CreateValidationSource(), new DirectorySource(profiledirectory, new DirectorySourceSettings()
                    {
                        IncludeSubDirectories = true,
                    })
                    ));
                    ValidationSettings settings = new ValidationSettings()
                    {
                        ResourceResolver = resolver,
                    };
                    Validator validator = new Validator(settings);
                    var outcome = validator.Validate(ProfileInstance);
                    if (outcome.Success == true)
                    {
                        isValid = true;
                        strError_OUT = "";
                    }
                    else
                    {
                        isValid = false;
                        FhirJsonSerializer serializer = new FhirJsonSerializer(new SerializerSettings()
                        {
                            Pretty = true,
                        });
                        string bundeljsonOutCome = serializer.SerializeToString(outcome);
                        JsonFileName = "Outcome.json";
                        File.WriteAllText(JsonFileName, bundeljsonOutCome);
                        strError_OUT = outcome.ToString();
                    }
                    return isValid;
                }
                else
                {
                    Console.WriteLine("Error while downloading pacakge");
                    return false;
                }
                #endregion


            }
            catch (Exception ex)
            {
                isValid = false;
                strError_OUT = ex.ToString();
                return isValid;
            }
        }
        public bool fnDownloadPackage(string URL)
        {
            try
            {
                bool isDownloaded = true;
                string parentDirName = new FileInfo(AppDomain.CurrentDomain.BaseDirectory).Directory.Parent.FullName;
                string path = parentDirName + "\\Debug\\";
                if (Directory.Exists(path + "\\package"))
                {
                    Directory.Delete(path + "\\package", true);
                }
                downloadfile(URL, path);
                return isDownloaded;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Inside fnDownloadPackage:Exception: " + ex.Message);
                return false;
            }
        }
        public void downloadfile(string URL, string path)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var req = client.GetAsync(URL).ContinueWith(res =>
                    {
                        var result = res.Result;
                        if (result.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = result.Content.ReadAsStreamAsync();
                            readData.Wait();

                            var readStream = readData.Result;

                            // Extract filestream and save in Output directory  
                            Stream inStream = readStream;
                            Stream gzipStream = new GZipInputStream(inStream);

                            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                            tarArchive.ExtractContents(path);
                            tarArchive.Close();

                            gzipStream.Close();
                            inStream.Close();
                        }
                    });
                    req.Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public Composition populatePrescriptionCompositionResource(PrescriptionCompositionFHIRDto dto)
        {
            // Set metadata about the resource - Version Id, Lastupdated Date, Profile

            Composition composition = new Composition()
            {
                Meta = new Meta()
                {
                    VersionId = "1",
                    LastUpdatedElement = new Instant(new DateTimeOffset(2020, 07, 09, 15, 32, 26, new TimeSpan(1, 0, 0))),
                    Profile = new List<string>()
                    {
                      "https://nrces.in/ndhm/fhir/r4/StructureDefinition/PrescriptionRecord",
                    },
                },
            };

            // Set logical id of this artifact
            composition.Id = dto.PrescriptionId.ToString("D");

            // Set language of the resource content
            composition.Language = "en-IN";

            // Set version-independent identifier for the Composition
            Identifier identifier = new Identifier();
            identifier.System = "https://ndhm.in/phr";
            identifier.Value = "645bb0c3-ff7e-4123-bef5-3852a4784813";


            composition.Identifier = identifier;

            // Status can be preliminary | final | amended | entered-in-error
            composition.Status = CompositionStatus.Final;


            // Kind of composition ("Prescription record ")
            var coding = new List<Coding>();
            coding.Add(new Coding("http://snomed.info/sct", "440545006", "Prescription record"));

            //old  - composition.Type = (new CodeableConcept("http://snomed.info/sct", "440545006", "Prescription record"));
            //composition.Type.Coding = coding;
            composition.Type = new CodeableConcept(
                "http://snomed.info/sct",
               "440545006",
               "Prescription record"
               );
            composition.Type.Coding.First().Display = "Prescription record"; // 

            // Set subject - Who and/or what the composition/Prescription record is about
            ResourceReference refrence = new ResourceReference();
            refrence.Reference = "urn:uuid:23986a85-fb64-48c2-ab85-3462586cc134";

            //composition.Subject = refrence;
            composition.Subject = new ResourceReference
            {
                Reference = $"urn:uuid:patient-{dto.PatientId}",
                Display = dto.PatientName
            };

            // Set Timestamp
            //composition.DateElement = new FhirDateTime("2017-05-27T11:46:09+05:30");
            if (dto.CreatedOn != null)
            {
            // #pragma warning disable CS0618
                composition.DateElement = new FhirDateTime(dto.CreatedOn.Value);
             // #pragma warning restore CS0618
            }


            // Set author - Who and/or what authored the composition/Presciption record
            composition.Author.Add(new ResourceReference
            {
                Reference = $"urn:uuid:practitioner-{dto.PractitionerId}",
                Display = dto.PractitionerName
            });

            // Set a Human Readable name/title
            composition.Title = "Prescription record";


            // Composition is broken into sections / Prescription record contains single section to define the relevant medication requests
            // Entry is a reference to data that supports this section
            ResourceReference reference1 = new ResourceReference();
            reference1.Reference = "urn:uuid:ee2af06a-903d-4387-8ed2-49f89d7da68d";
            reference1.Type = "MedicationRequest";


            //section.Code.Coding = new List<Coding>();
            //var codeitem = new Coding();
            //codeitem.System = "http://snomed.info/sct";
            //codeitem.Code = "440545006";
            //codeitem.Display = "Prescription record";

            //section.Code.Coding.Add(codeitem);

            //section.Entry.Add(reference1);
            ////section.Entry.Add(reference2);
            ////section.Entry.Add(reference3);

            //composition.Section.Add(section);
            var section = new Composition.SectionComponent
            {
                Title = "Prescription record",
                Code = new CodeableConcept
                {
                    Coding = new List<Coding>
                  {
                      new Coding
                        {
                         System = "http://snomed.info/sct",
                          Code = "440545006",
                          Display = "Prescription record" // REQUIRED
                         }
                  },
                    Text = "Prescription record"
                }
            };


            if (dto.MedicationItemIds != null && dto.MedicationItemIds.Any())
            {
                foreach (var itemId in dto.MedicationItemIds)
                {
                    section.Entry.Add(new ResourceReference
                    {
                        Reference = $"urn:uuid:medicationrequest-{dto.PrescriptionId}-{itemId}",
                        Type = "MedicationRequest"
                    });
                }
            }

            composition.Section.Add(section);

            return composition;
        }


        public Patient populatePatientResource(PatientFHIRDto dto)
        {
            Patient patient = new Patient()
            {

                Meta = new Meta()
                {
                    Profile = new List<string>()
                    {
                      "https://nrces.in/ndhm/fhir/r4/StructureDefinition/Patient",
                    },
                    VersionId = "1",
                    LastUpdatedElement = new Instant(new DateTimeOffset(2020, 07, 09, 15, 32, 26, new TimeSpan(1, 0, 0)))
                },
            };

            patient.Id = $"patient-{dto.PatientId}";

            var id = new Identifier();
            id.System = "https://ndhm.in/SwasthID";
            id.Value = "1234";
            id.Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "MR", "Medical record number", "Text");
            patient.Identifier.Add(id);

            var name = new HumanName();
            name.Text = dto.FullName ?? "Unknown Patient";
            patient.Name.Add(name);

            if (!string.IsNullOrEmpty(dto.Gender))
            {
                if (dto.Gender.Equals("male", StringComparison.OrdinalIgnoreCase))
                    patient.Gender = AdministrativeGender.Male;
                else if (dto.Gender.Equals("female", StringComparison.OrdinalIgnoreCase))
                    patient.Gender = AdministrativeGender.Female;
                else
                    patient.Gender = AdministrativeGender.Unknown;
            }
            if (dto.BirthDate.HasValue)
                patient.BirthDate = dto.BirthDate.Value.ToString("yyyy-MM-dd");

            if (!string.IsNullOrEmpty(dto.Phone))
            {
                ContactPoint contact1 = new ContactPoint();
                contact1.System = ContactPoint.ContactPointSystem.Phone;
                contact1.Value = dto.Phone;
                contact1.Use = ContactPoint.ContactPointUse.Home;
                patient.Telecom.Add(contact1);
            }
            Address address = new Address();
            address.Text = dto.Address ?? "";
            address.State = dto.State ?? "";
            address.Country = dto.CountryCode ?? "IN";
            patient.Address.Add(address);

            return patient;
        }

        public Practitioner populatePractitionerResource(PractitionerFHIRDto dto)
        {
            Practitioner practitioner = new Practitioner()
            {
                Meta = new Meta()
                {
                    VersionId = "1",
                    LastUpdatedElement = new Instant(new DateTimeOffset(2019, 05, 29, 14, 58, 18, new TimeSpan(1, 0, 0))),

                    Profile = new List<string>()
                    {
                       "https://nrces.in/ndhm/fhir/r4/StructureDefinition/Practitioner",
                    },
                }
            };
            practitioner.Id = $"practitioner-{dto.PractitionerId}";

            var coding = new List<Coding>();
            coding.Add(new Coding(
                "http://terminology.hl7.org/CodeSystem/v2-0203",
                "MD",
                "Medical License number"
            ));

            Identifier identifier = new Identifier();
            identifier.System = "https://doctor.ndhm.gov.in";
            identifier.Value = "21-1521-3828-3227";
            identifier.Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "MD", "Medical License number");
            identifier.Type.Coding = coding;
            practitioner.Identifier.Add(identifier);

            var name = new HumanName();
            name.Text = dto.FullName ?? "Unknown Practitioner";
            practitioner.Name.Add(name);

            if (!string.IsNullOrEmpty(dto.Gender))
            {
                if (dto.Gender.Equals("male", StringComparison.OrdinalIgnoreCase))
                    practitioner.Gender = AdministrativeGender.Male;
                else if (dto.Gender.Equals("female", StringComparison.OrdinalIgnoreCase))
                    practitioner.Gender = AdministrativeGender.Female;
                else
                    practitioner.Gender = AdministrativeGender.Unknown;
            }
            return practitioner;
        }

        public MedicationRequest populateMedicationRequestResource(MedicationRequestFHIRDto dto, string resourceId)

        {
            MedicationRequest medicationRequest = new MedicationRequest()
            {

                Meta = new Meta()
                {
                    VersionId = "1",
                    LastUpdatedElement = new Instant(new DateTimeOffset(2020, 07, 09, 15, 32, 26, new TimeSpan(1, 0, 0))),
                    Profile = new List<string>()
                    {
                      "https://nrces.in/ndhm/fhir/r4/StructureDefinition/MedicationRequest",
                    },

                },
            };
            medicationRequest.Id = medicationRequest.Id = resourceId;


            medicationRequest.Status = MedicationRequest.medicationrequestStatus.Active;
            medicationRequest.Intent = MedicationRequest.medicationRequestIntent.Order;

            medicationRequest.Medication = new CodeableConcept
            {
                Coding = new List<Coding>
            {
                new Coding
               {
                 System = "http://snomed.info/sct",
                 Code = dto.MedicationCode ?? "353231006",
                 Display = dto.MedicationName ?? "Unknown medication"
               }
            },
                Text = dto.MedicationName ?? "Unknown medication"
            };


            if (dto.AuthoredOn.HasValue)
            {
                medicationRequest.AuthoredOnElement = new FhirDateTime(dto.AuthoredOn.Value);
            }
            medicationRequest.Subject = new ResourceReference(
              $"urn:uuid:patient-{dto.PatientId}",
              dto.PatientName ?? "Unknown Patient"
             );

            medicationRequest.Requester = new ResourceReference(
            $"urn:uuid:practitioner-{dto.PractitionerId}",
            dto.PractitionerName ?? "Unknown Practitioner"
            );


            //medicationRequest.ReasonReference.Add(new ResourceReference(
            // $"urn:uuid:composition-{dto.PrescriptionId}"
            //  ));

            var dosage = new Dosage
            {
                Text = dto.DosageText ?? "No dosage specified",
                Timing = new Timing
                {
                    Repeat = new Timing.RepeatComponent
                    {
                        Frequency = dto.Frequency,
                        Period = dto.Period ?? 1,
                        PeriodUnit = Timing.UnitsOfTime.D
                    }
                },
                Route = new CodeableConcept
                {
                    Coding = new List<Coding>
        {
            new Coding
                {
                   System = "http://snomed.info/sct",
                   Code = "6064005",
                   Display = dto.RouteDisplay ?? "Oral route" // ✅ REQUIRED
                 }
                },
                    Text = dto.RouteDisplay ?? "Oral route"
                },
                     Method = new CodeableConcept
                  {
                    Coding = new List<Coding>
                  {
                    new Coding
                    {
                      System = "http://snomed.info/sct",
                      Code = "421521009",
                      Display = "Swallow" // ✅ REQUIRED
                    }
                    },
                    Text = "Swallow"
                }
            };


            dosage.AdditionalInstruction.Add(new CodeableConcept
            {
                Coding = new List<Coding>
            {
                  new Coding
               {
                  System = "http://snomed.info/sct",
                  Code = "229799001",
                  Display = dto.FrequencyDisplay ?? "Once a day"
                }
            },
                Text = dto.FrequencyDisplay ?? "Once a day"
            });


            medicationRequest.DosageInstruction.Add(dosage);

            return medicationRequest;
        }


        public  Binary populateBinaryResource()
        {
            Binary binary = new Binary()
            {

                Meta = new Meta()
                {
                    VersionId = "1",
                    LastUpdatedElement = new Instant(new DateTimeOffset(2020, 07, 09, 15, 32, 26, new TimeSpan(1, 0, 0))),
                    Profile = new List<string>()
                    {
                      "https://nrces.in/ndhm/fhir/r4/StructureDefinition/Binary",
                    },

                },
            };

            binary.Id = "859a3e51-5027-486a-bb41-c7773300fd40";
            binary.ContentType = "application/pdf";
            string author = "R0lGODlhfgCRAPcAAAAAAIAAAACAAICAAAAAgIAA oxrXyMY2uvGNcIyj    HOeoxkXBh44OOZdn8Ggu+DiPjwtJ2CZyUomCTRGO";
            // converts a C# string to a byte array
            byte[] bytes = Encoding.ASCII.GetBytes(author);
            binary.Data = bytes;
            return binary;
        }

        // Populate Signature Resource
        public  Signature populateSignature()
        {
            Signature signature = new Signature();
            Coding item1 = new Coding();
            item1.System = "urn:iso-astm:E1762-95:2013";
            item1.Code = "1.2.840.10065.1.12.1.1";
            item1.Display = "Author's Signature";
            signature.Type.Add(item1);
            signature.When = new DateTime(2020, 07, 09, 07, 42, 33);
            signature.Who = new ResourceReference("urn:uuid:86c1ae40-b60e-49b5-b2f4-a217bcd19147");
            signature.SigFormat = "image/jpeg";
            string data = "R0lGODlhfgCRAPcAAAAAAIAAAACAAICAAAAAgIAA oxrXyMY2uvGNcIyj    HOeoxkXBh44OOZdn8Ggu+DiPjwtJ2CZyUomCTRGO";
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            signature.Data = bytes;

            return signature;
        }





        // Other populate methods...
    }

}

