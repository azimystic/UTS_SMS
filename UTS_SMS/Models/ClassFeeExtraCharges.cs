using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class ClassFeeExtraCharges
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ChargeName { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive value")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } // OncePerClass, OncePerLifetime, MonthlyCharges

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // If null, applies to all classes; if set, applies to specific class
        public int? ClassId { get; set; }
        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Navigation property for excluded students
        public virtual ICollection<ClassFeeExtraChargeExclusion> ExcludedStudents { get; set; } = new List<ClassFeeExtraChargeExclusion>();
    }

    public enum ChargeCategory
    {
        OncePerClass,
        OncePerLifetime,
        MonthlyCharges
    }
}
