using System.ComponentModel.DataAnnotations;

namespace SMS.ViewModels
{
    public class BillingCreateViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public decimal MiscallaneousCharges { get; set; }


        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public int ForMonth { get; set; }  

        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int ForYear { get; set; }
        public string? RemarksPreviousDues { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal PreviousDues { get; set; }
        public decimal Fine { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cash paid cannot be negative")]
        public decimal CashPaid { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Online paid cannot be negative")]
        public decimal OnlinePaid { get; set; }

        public int? OnlineAccount { get; set; }

        [Required(ErrorMessage = "Payment date is required")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Received by is required")]
        public string ReceivedBy { get; set; } = "Azeem";

        // Extra charges
        public decimal ExtraCharges { get; set; }
        public List<ExtraChargeItem> ExtraChargeItems { get; set; } = new List<ExtraChargeItem>();

        // Salary deduction (for employee parent students)
        public decimal SalaryDeductionAmount { get; set; }

        // Calculated properties for display
        public decimal TotalPayable { get; set; }
        public decimal TotalPaid => CashPaid + OnlinePaid;
        public decimal Dues => TotalPayable - TotalPaid;
    }

    public class ExtraChargeItem
    {
        public int ChargeId { get; set; }
        public string ChargeName { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
    }
}