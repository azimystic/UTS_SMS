using UTS_SMS.Models;

namespace UTS_SMS.ViewModels
{
    public class BillingReportViewModel
    {
        public Student Student { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } // Paid, Partial, NotPaid, NotProcessed
        public List<BillingMaster> BillingRecords { get; set; } = new List<BillingMaster>();
    }

    public class BillingDetailReportViewModel
    {
        public Student Student { get; set; }
        public List<BillingMaster> BillingRecords { get; set; } = new List<BillingMaster>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPayable => BillingRecords.Sum(b => b.TuitionFee + b.AdmissionFee + b.Fine + b.PreviousDues + b.MiscallaneousCharges);
         public decimal TotalPaid => BillingRecords
    .SelectMany(b => b.Transactions)
    .Sum(t => t.AmountPaid);
        public decimal Balance
        {
            get
            {
                var lastBilling = BillingRecords
                    .OrderByDescending(b => b.ForYear)
                    .ThenByDescending(b => b.ForMonth)
                    .FirstOrDefault();

                return lastBilling == null ? 0 : lastBilling.Dues;
            }
        }
    }

    public class TransactionReportViewModel
    {
        public List<BillingTransaction> Transactions { get; set; } = new List<BillingTransaction>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount => Transactions.Sum(t => t.AmountPaid);
        public decimal TotalCash => Transactions.Sum(t => t.CashPaid);
        public decimal TotalOnline => Transactions.Sum(t => t.OnlinePaid);
    }
}