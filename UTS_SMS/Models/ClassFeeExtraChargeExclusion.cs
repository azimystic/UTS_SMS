using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class ClassFeeExtraChargeExclusion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassFeeExtraChargeId { get; set; }
        [ForeignKey("ClassFeeExtraChargeId")]
        public ClassFeeExtraCharges ClassFeeExtraCharge { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        public DateTime ExcludedDate { get; set; } = DateTime.Now;
        public string ExcludedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
