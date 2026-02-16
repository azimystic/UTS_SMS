using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class SalaryDeduction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public int BillingMasterId { get; set; }
        [ForeignKey("BillingMasterId")]
        public BillingMaster BillingMaster { get; set; }

        [Required]
        public decimal AmountDeducted { get; set; }

        [Required]
        public int ForMonth { get; set; }   // 1 = January, etc.

        [Required]
        public int ForYear { get; set; }

        public DateTime DeductionDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
