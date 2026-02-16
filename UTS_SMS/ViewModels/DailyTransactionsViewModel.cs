using UTS_SMS.Models;

namespace SMS.ViewModels
{
    public class DailyTransactionsViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public Campus? Campus { get; set; }
        public List<GradeTransactionGroup> GradeGroups { get; set; } = new List<GradeTransactionGroup>();
        public decimal GrandTotal => GradeGroups.Sum(g => g.TotalAmount);
        public decimal TotalCash => GradeGroups.Sum(g => g.TotalCash);
        public decimal TotalOnline => GradeGroups.Sum(g => g.TotalOnline);
        public int TotalTransactions => GradeGroups.Sum(g => g.Transactions.Count);
    }

    public class GradeTransactionGroup
    {
        public Class? ClassObj { get; set; }
        public List<StudentTransactionGroup> StudentGroups { get; set; } = new List<StudentTransactionGroup>();
        public decimal TotalAmount => StudentGroups.Sum(s => s.TotalAmount);
        public decimal TotalCash => StudentGroups.Sum(s => s.TotalCash);
        public decimal TotalOnline => StudentGroups.Sum(s => s.TotalOnline);
        public List<BillingTransaction> Transactions => StudentGroups.SelectMany(s => s.Transactions).ToList();
    }

    public class StudentTransactionGroup
    {
        public Student? Student { get; set; }
        public List<BillingTransaction> Transactions { get; set; } = new List<BillingTransaction>();
        public decimal TotalAmount => Transactions.Sum(t => t.AmountPaid);
        public decimal TotalCash => Transactions.Sum(t => t.CashPaid);
        public decimal TotalOnline => Transactions.Sum(t => t.OnlinePaid);
    }
}