using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UTS_SMS.Services
{
    /// <summary>
    /// Service for sending WhatsApp messages via webhook API
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WhatsAppService> _logger;
        private const string WEBHOOK_URL = "http://erp.visionplusapps.com:5678/webhook-test/send";
        private const string API_KEY = "HELLOMYNAMEISAZEEM23092005";
        private const string BASIC_AUTH_USER = "admin";
        private const string BASIC_AUTH_PASS = "admin@123";
        private const string CLIENT_NAME = "imHhGgIQNYYmaLye";

        public WhatsAppService(IHttpClientFactory httpClientFactory, ILogger<WhatsAppService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Send a WhatsApp message via the webhook API
        /// </summary>
        public async Task<bool> SendMessageAsync(string clientName, string phone, string message)
        {
            try
            {
                // Format phone number to ensure it starts with 92
                var formattedPhone = FormatPhoneNumber(phone);
                
                if (string.IsNullOrWhiteSpace(formattedPhone))
                {
                    _logger.LogWarning("Invalid phone number: {Phone}", phone);
                    return false;
                }

                // Create the request payload - always use hardcoded client name
                var payload = new
                {
                    clientName = CLIENT_NAME,
                    phone = formattedPhone,
                    message = message,
                    isMedia = false
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();
                
                // Add Basic Authentication header
                var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{BASIC_AUTH_USER}:{BASIC_AUTH_PASS}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                
                // Add API Key header
                client.DefaultRequestHeaders.Add("apikey", API_KEY);

                // Send the request
                var response = await client.PostAsync(WEBHOOK_URL, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp message sent successfully to {Phone}", formattedPhone);
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to send WhatsApp message to {Phone}. Status: {Status}, Response: {Response}", 
                        formattedPhone, response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", phone);
                return false;
            }
        }

        /// <summary>
        /// Format phone number to ensure it starts with 92
        /// Converts 0300... to 92300...
        /// </summary>
        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Remove all non-digit characters
            phone = new string(phone.Where(char.IsDigit).ToArray());

            // If starts with 0, replace with 92
            if (phone.StartsWith("0"))
            {
                phone = "92" + phone.Substring(1);
            }
            // If doesn't start with 92, add it
            else if (!phone.StartsWith("92"))
            {
                phone = "92" + phone;
            }

            return phone;
        }
    }
}
