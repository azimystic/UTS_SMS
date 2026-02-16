using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class PayrollTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PayrollMasterId { get; set; }

        [Required]
        public decimal AmountPaid { get; set; }

        public decimal CashPaid { get; set; } = 0;
        public decimal OnlinePaid { get; set; } = 0;

        public int? OnlineAccount { get; set; }

        [ForeignKey("OnlineAccount")]
        public BankAccount Account { get; set; }

        public string? TransactionReference { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string ReceivedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        // Navigation property
        [ForeignKey("PayrollMasterId")]
        public virtual PayrollMaster PayrollMaster { get; set; }

        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}