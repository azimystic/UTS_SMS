using UTS_SMS.Models;

namespace UTS_SMS.ViewModels
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
