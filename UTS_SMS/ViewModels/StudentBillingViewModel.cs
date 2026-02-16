using SMS.Models;

namespace SMS.ViewModels
{
    public class StudentBillingViewModel
    {
        public Student Student { get; set; }
        public BillingMaster LatestBilling { get; set; }
        public string RowColor { get; set; }
        public bool CanPay { get; set; }
        public decimal Dues { get; set; }
    }
}
