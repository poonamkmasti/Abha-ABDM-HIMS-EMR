using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asp.netWebAPP.Core.Domain.Model
{
    public class PHRMPrescriptionModel
    {
        [Key]
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public int? PrescriberId { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string PrescriberName { get; set; }
        public bool? IsInPatient { get; set; }
        public string PrescriptionStatus { get; set; }
        public virtual List<PHRMPrescriptionItemModel> PHRMPrescriptionItems { get; set; }
        [NotMapped]
        public string PatientName { get; set; }
        public int PatientVisitId { get; set; }
        public int PrescriptionNo { get; set; }
    }
}
