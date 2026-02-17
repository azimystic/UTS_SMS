namespace UTS_SMS.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateTransactionReceiptPdfAsync(int transactionId);
    }
}
