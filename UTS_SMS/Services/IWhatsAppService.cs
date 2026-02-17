namespace UTS_SMS.Services
{
    /// <summary>
    /// Interface for WhatsApp messaging service
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// Send a WhatsApp message via the webhook API
        /// </summary>
        /// <param name="clientName">Name of the client/recipient</param>
        /// <param name="phone">Phone number (should start with 92)</param>
        /// <param name="message">Message content to send</param>
        /// <returns>True if message sent successfully, false otherwise</returns>
        Task<bool> SendMessageAsync(string clientName, string phone, string message);
    }
}
