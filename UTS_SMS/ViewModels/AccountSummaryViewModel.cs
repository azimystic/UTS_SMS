using UTS_SMS.Models;

namespace UTS_SMS.ViewModels
{
    public class AccountSummaryViewModel
    {
        public BankAccount Account { get; set; } = new();
        public int Month { get; set; }
        public int Year { get; set; }
        
        // Money flows
        public decimal FeesReceived { get; set; }
        public decimal PayrollPaid { get; set; }
        public decimal ExpensesPaid { get; set; }
        public decimal AssetsPurchased { get; set; }
        public decimal NetFlow { get; set; }
        
        // Transaction counts
        public int FeesTransactionCount { get; set; }
        public int PayrollTransactionCount { get; set; }
        public int ExpensesTransactionCount { get; set; }
        public int AssetsTransactionCount { get; set; }
        
        public int TotalTransactions => FeesTransactionCount + PayrollTransactionCount + 
                                       ExpensesTransactionCount + AssetsTransactionCount;
        
        public decimal TotalIncome => FeesReceived;
        public decimal TotalExpenses => PayrollPaid + ExpensesPaid + AssetsPurchased;
        
        public string MonthName => Month switch
        {
            1 => "January", 2 => "February", 3 => "March", 4 => "April",
            5 => "May", 6 => "June", 7 => "July", 8 => "August",
            9 => "September", 10 => "October", 11 => "November", 12 => "December",
            _ => "Unknown"
        };
    }

    public class AccountTransactionDetailViewModel
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
        public string? Reference { get; set; }
        
        public decimal NetAmount => AmountIn - AmountOut;
    }
}