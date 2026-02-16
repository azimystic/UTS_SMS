using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class LeaveBalanceHistory
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
        [MaxLength(100)]
        public string ActionType { get; set; } // Allocated, Used, CarriedForward, RoleChange, Adjustment

        [Required]
        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int? LeaveRequestId { get; set; }
        [ForeignKey("LeaveRequestId")]
        public LeaveRequest? LeaveRequest { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
