using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class SalaryDefinition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Basic salary must be a positive value")]
        public decimal BasicSalary { get; set; }

        // Allowances
        public decimal HouseRentAllowance { get; set; } = 0;
        public decimal MedicalAllowance { get; set; } = 0;
        public decimal TransportationAllowance { get; set; } = 0;
        public decimal OtherAllowances { get; set; } = 0;

        // Deductions
        public decimal ProvidentFund { get; set; } = 0;
        public decimal TaxDeduction { get; set; } = 0;
        public decimal OtherDeductions { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
         

        // Calculated property
        [NotMapped]
        public decimal GrossSalary => BasicSalary + HouseRentAllowance + MedicalAllowance +
                                     TransportationAllowance + OtherAllowances;

        [NotMapped]
        public decimal TotalAllowances => HouseRentAllowance + MedicalAllowance +
                                          TransportationAllowance + OtherAllowances;

        [NotMapped]
        public decimal TotalDeductions => ProvidentFund + TaxDeduction + OtherDeductions;

        [NotMapped]
        public decimal NetSalary => GrossSalary - TotalDeductions;
    }
}
 