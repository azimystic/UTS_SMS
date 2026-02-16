// View Models
using UTS_SMS.Models;

namespace SMS.ViewModels
{
    public class PayrollReportViewModel
    {
        public PayrollMaster PayrollMaster { get; set; }
        public Employee Employee { get; set; }
        public Campus Campus { get; set; }
        public string Status { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
    }

    public class PayrollDetailReportViewModel
    {
        public Employee Employee { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PayrollMaster> PayrollRecords { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public PayrollMaster LatestPayroll { get; set; }
    }

    // ✅ New ViewModel for Transaction Receipt
    public class PayrollReceiptViewModel
    {
        public PayrollMaster Master { get; set; }
        public List<PayrollTransaction> Transactions { get; set; }
    }
}
