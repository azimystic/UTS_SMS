using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class PayrollMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int ForMonth { get; set; }   // 1 = January, etc.

        [Required]
        public int ForYear { get; set; }

        // Salary components
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; } = 0;
        public decimal Deductions { get; set; } = 0;
        public decimal Bonus { get; set; } = 0;
         public decimal PreviousBalance { get; set; } = 0;
        public decimal AttendanceDeduction { get; set; } = 0;

        // Calculated properties
        public decimal GrossSalary => BasicSalary + Allowances;
        public decimal NetSalary => GrossSalary - Deductions - AttendanceDeduction + Bonus  + PreviousBalance;

        // Payment status
        public decimal AmountPaid { get; set; } = 0;
        public decimal Balance => NetSalary - AmountPaid;

        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedDate { get; set; }
        public string? ApprovedBy { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
        public virtual ICollection<PayrollTransaction> Transactions { get; set; } = new List<PayrollTransaction>();
        public virtual ICollection<EmployeeAttendance> AttendanceRecords { get; set; } = new List<EmployeeAttendance>();
    }
}