using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class Family
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FatherName { get; set; }

        [Required]
        [StringLength(50)]
        public string FatherCNIC { get; set; }

        // Email field for parent account creation
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? FatherPhone { get; set; }

        [StringLength(100)]
        public string? MotherName { get; set; }

        [StringLength(50)]
        public string? MotherCNIC { get; set; }

        [StringLength(15)]
        public string? MotherPhone { get; set; }

        [StringLength(200)]
        public string? HomeAddress { get; set; }

        public string? FatherSourceOfIncome { get; set; }

        public bool IsFatherDeceased { get; set; } = false;

        [StringLength(100)]
        public string? GuardianName { get; set; }

        [StringLength(15)]
        public string? GuardianPhone { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Navigation property for students in this family
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        
        // Navigation property for parent user account
        public virtual ApplicationUser? ParentUser { get; set; }
    }
}