using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeaveType { get; set; } // Sick Leave, Casual Leave, Annual Leave, etc.

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }

        [NotMapped]
        public int TotalDays 
        { 
            get 
            {
                int workingDays = 0;
                for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
                {
                    // Count all days except Sunday (Saturday is now included)
                    if (date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        workingDays++;
                    }
                }
                return workingDays;
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
