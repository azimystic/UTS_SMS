using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class StudentCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryType { get; set; } // Regular, Disabled, EmployeeParent, Sibling, Alumni, Custom

        public bool IsSystemDefined { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Default discount configuration (only for non-EmployeeParent types)
        public decimal DefaultAdmissionFeeDiscount { get; set; } = 0;
        public decimal DefaultTuitionFeeDiscount { get; set; } = 0;

        // For sibling discounts
        public int? SiblingCount { get; set; }
        public decimal? PerSiblingAdmissionDiscount { get; set; }
        public decimal? PerSiblingTuitionDiscount { get; set; }

        // For employee discounts
        public virtual ICollection<EmployeeCategoryDiscount> EmployeeCategoryDiscounts { get; set; } = new List<EmployeeCategoryDiscount>();
    }

    public class EmployeeCategoryDiscount
    {
        [Key]
        public int Id { get; set; }

        public int StudentCategoryId { get; set; }
        [ForeignKey("StudentCategoryId")]
        public StudentCategory StudentCategory { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeCategory { get; set; }

        public decimal AdmissionFeeDiscount { get; set; } = 0;
        public decimal TuitionFeeDiscount { get; set; } = 0;
    }

    public class StudentCategoryAssignment
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        public int StudentCategoryId { get; set; }
        [ForeignKey("StudentCategoryId")]
        public StudentCategory StudentCategory { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [StringLength(20)]
        public string PaymentMode { get; set; }

        public decimal? CustomAdmissionPercent { get; set; }
        public decimal? CustomTuitionPercent { get; set; }

        public decimal AppliedAdmissionFeeDiscount { get; set; }
        public decimal AppliedTuitionFeeDiscount { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;
        public string AssignedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
