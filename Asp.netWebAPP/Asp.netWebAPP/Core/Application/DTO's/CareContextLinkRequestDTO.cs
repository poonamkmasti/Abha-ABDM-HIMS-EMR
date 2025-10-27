//namespace Asp.netWebAPP.Core.Application.DTO_s
//{
//    public class CareContextLinkRequestDTO
//    {
//        public string AbhaNumber { get; set; }
//        public string AbhaAddress { get; set; }
//        public List<PatientDTO> Patient { get; set; }
//    }

//    public class PatientDTO
//    {
//        public string ReferenceNumber { get; set; }
//        public string Display { get; set; }
//        public List<CareContextDTO> CareContexts { get; set; }
//        public string HiType { get; set; }
//        public int Count { get; set; }
//    }
//    public class CareContextDTO
//    {
//        public string ReferenceNumber { get; set; }
//        public string Display { get; set; }
//    }

//}
namespace Asp.netWebAPP.Core.Application.DTO_s
{
    using System.ComponentModel.DataAnnotations;
    // 1. Add this namespace
    using System.Text.Json.Serialization;

    public class CareContextLinkRequestDTO
    {
        [Required(ErrorMessage = "ABHA Number is required.")]
        [JsonPropertyName("abhaNumber")]
        public string AbhaNumber { get; set; }

        [Required(ErrorMessage = "ABHA Address is required.")]
        [JsonPropertyName("abhaAddress")]
        public string AbhaAddress { get; set; }

        // 2. Map the C# 'Patient' property to the JSON key 'patient'
        [Required(ErrorMessage = "Patient details are required.")]
        [MinLength(1, ErrorMessage = "Patient list cannot be empty.")]
        [JsonPropertyName("patient")]
        public List<PatientDTO> Patient { get; set; }
    }

    public class PatientDTO
    {
        // 3. Map C# properties to their camelCase JSON keys
        [Required(ErrorMessage = "Patient ReferenceNumber is required.")]
        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; }


        [JsonPropertyName("display")]
        public string Display { get; set; }

        [Required(ErrorMessage = "Care Context list is required.")]
        [MinLength(1, ErrorMessage = "Care Context list cannot be empty.")]
        [JsonPropertyName("careContexts")]
        public List<CareContextDTO> CareContexts { get; set; }

        [JsonPropertyName("hiType")]
        public string HiType { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class CareContextDTO
    {
        // 4. Map C# properties to their camelCase JSON keys
        [Required(ErrorMessage = "Care Context ReferenceNumber is required.")]
        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; }

        [JsonPropertyName("display")]
        public string Display { get; set; }
    }
}
