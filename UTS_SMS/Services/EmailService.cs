using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace UTS_SMS.Services
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName, string username, string password, string role);
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendFeeReceiptEmailAsync(string toEmail, string studentName, decimal amountPaid, string receiptUrl, DateTime paymentDate, string transactionRef);
        Task<bool> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] pdfAttachment, string attachmentFileName);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName, string username, string password, string role)
        {
            var subject = $"Welcome to School Management System - Your {role} Account";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .credentials {{ background: white; padding: 20px; border-left: 4px solid #667eea; margin: 20px 0; }}
        .credential-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #667eea; }}
        .value {{ color: #333; font-family: monospace; background: #f0f0f0; padding: 5px 10px; border-radius: 5px; display: inline-block; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .button {{ background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Welcome to SMS</h1>
            <p>School Management System</p>
        </div>
        <div class='content'>
            <h2>Hello {fullName}!</h2>
            <p>Your account has been successfully created. You can now access the School Management System portal.</p>
            
            <div class='credentials'>
                <h3>Your Login Credentials</h3>
                <div class='credential-row'>
                    <span class='label'>Username:</span>
                    <span class='value'>{username}</span>
                </div>
                <div class='credential-row'>
                    <span class='label'>Email:</span>
                    <span class='value'>{toEmail}</span>
                </div>
                <div class='credential-row'>
                    <span class='label'>Password:</span>
                    <span class='value'>{password}</span>
                </div>
                <div class='credential-row'>
                    <span class='label'>Role:</span>
                    <span class='value'>{role}</span>
                </div>
            </div>

            <div class='warning'>
                <strong>?? Important Security Notice:</strong>
                <ul>
                    <li>Please change your password after first login</li>
                    <li>Do not share your credentials with anyone</li>
                    <li>Keep this email secure and delete it after changing your password</li>
                </ul>
            </div>

            <center>
                <a href='https://yourdomain.com/Account/Login' class='button'>Login to Portal</a>
            </center>

            <h3>What's Next?</h3>
            <ol>
                <li>Click the login button above or visit the school portal</li>
                <li>Enter your username and password</li>
                <li>Update your profile and change your password</li>
                <li>Upload your profile picture</li>
            </ol>

            <p>If you have any questions or need assistance, please contact the school administration.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} School Management System. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                // If email settings are not configured, log to console instead
                if (string.IsNullOrEmpty(smtpHost))
                {
                    _logger.LogWarning("Email settings not configured. Email would be sent to: {Email}", toEmail);
                    _logger.LogInformation("Subject: {Subject}", subject);
                    _logger.LogInformation("Body: {Body}", body);
                    return true;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendFeeReceiptEmailAsync(string toEmail, string studentName, decimal amountPaid, string receiptUrl, DateTime paymentDate, string transactionRef)
        {
            var subject = $"Fee Payment Receipt - {studentName}";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .payment-details {{ background: white; padding: 20px; border-left: 4px solid #4CAF50; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; display: flex; justify-content: space-between; }}
        .label {{ font-weight: bold; color: #667eea; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 24px; color: #4CAF50; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .button {{ background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .success-badge {{ background: #4CAF50; color: white; padding: 10px 20px; border-radius: 20px; display: inline-block; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Payment Successful</h1>
            <p>School Fee Receipt</p>
        </div>
        <div class='content'>
            <div class='success-badge'>
                Payment Confirmed
            </div>
            
            <h2>Dear Parent/Guardian,</h2>
            <p>Thank you for your payment. We have successfully received your school fee payment.</p>
            
            <div class='payment-details'>
                <h3 style='color: #667eea; margin-top: 0;'>Payment Details</h3>
                <div class='detail-row'>
                    <span class='label'>Student Name:</span>
                    <span class='value'>{studentName}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Amount Paid:</span>
                    <span class='amount'>Rs. {amountPaid:N2}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Payment Date:</span>
                    <span class='value'>{paymentDate:dd MMM yyyy, hh:mm tt}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Transaction Ref:</span>
                    <span class='value'>{transactionRef}</span>
                </div>
            </div>

            <center>
                <a href='{receiptUrl}' class='button' style='color: white;'>View Receipt</a>
            </center>

            <p><strong>Important Notes:</strong></p>
            <ul>
                <li>This is an automated email confirmation of your payment</li>
                <li>Please keep this email for your records</li>
                <li>You can access and print your receipt using the button above</li>
                <li>For any queries, please contact the school administration</li>
            </ul>

            <p>Thank you for your continued trust and support.</p>
            <p><strong>Best regards,</strong><br/>School Administration</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} School Management System. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] pdfAttachment, string attachmentFileName)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                // If email settings are not configured, log to console instead
                if (string.IsNullOrEmpty(smtpHost))
                {
                    _logger.LogWarning("Email settings not configured. Email would be sent to: {Email}", toEmail);
                    _logger.LogInformation("Subject: {Subject}", subject);
                    _logger.LogInformation("Body: {Body}", body);
                    return true;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Add attachment
                if (pdfAttachment != null && pdfAttachment.Length > 0 && !string.IsNullOrEmpty(attachmentFileName))
                {
                    var ms = new MemoryStream(pdfAttachment);
                    mailMessage.Attachments.Add(new Attachment(ms, attachmentFileName, "application/pdf"));
                }

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email with attachment sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with attachment to {Email}", toEmail);
                return false;
            }
        }
    }
}
