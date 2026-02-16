using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class LeaveBalance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeaveType { get; set; }

        [Required]
        public int Year { get; set; }

        public int? Month { get; set; } // Null for yearly leaves

        [Required]
        public decimal TotalAllocated { get; set; }

        [Required]
        public decimal Used { get; set; } = 0;

        [Required]
        public decimal CarriedForward { get; set; } = 0;

        [NotMapped]
        public decimal Available 
        { 
            get 
            {
                return TotalAllocated + CarriedForward - Used;
            }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
