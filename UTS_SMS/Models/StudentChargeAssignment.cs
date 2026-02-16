using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class StudentChargeAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int ClassFeeExtraChargeId { get; set; }
        [ForeignKey("ClassFeeExtraChargeId")]
        public ClassFeeExtraCharges ClassFeeExtraCharge { get; set; }

        public bool IsAssigned { get; set; } = true;

        public DateTime AssignedDate { get; set; } = DateTime.Now;
        public string AssignedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
