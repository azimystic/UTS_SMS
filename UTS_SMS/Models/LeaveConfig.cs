using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class LeaveConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EmployeeType { get; set; } // Teacher, Admin, Accountant, etc.

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; } // Mandatory: specific role from EmployeeRoleConfig

        [Required]
        [MaxLength(50)]
        public string LeaveType { get; set; } // Sick Leave, Casual Leave, Annual Leave, etc.

        [Required]
        [MaxLength(20)]
        public string AllocationPeriod { get; set; } // Monthly, Yearly

        [Required]
        [Range(0, 365)]
        public int AllowedDays { get; set; }

        public bool IsCarryForward { get; set; } = false; // Can unused leaves be carried forward?

        [Range(0, 365)]
        public int? MaxCarryForwardDays { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
