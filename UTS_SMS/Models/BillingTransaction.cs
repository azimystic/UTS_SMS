using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    public class BillingTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BillingMasterId { get; set; }

        [Required]
        public decimal AmountPaid { get; set; } 

        public decimal CashPaid { get; set; } = 0;
        public decimal OnlinePaid { get; set; } = 0;
 

        public int? OnlineAccount { get; set; }
        public BankAccount Account { get; set; }
        public string? TransactionReference { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string ReceivedBy { get; set; }

        // Navigation property
        public virtual BillingMaster BillingMaster { get; set; }
        public int CampusId { get; set; }
        public Campus Campus { get; set; }
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Online = 2,
        BankTransfer = 3,
        Cheque = 4
    }
}