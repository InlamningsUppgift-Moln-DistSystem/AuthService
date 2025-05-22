using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AuthService.Helpers
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SendGridEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:From"];
            var fromName = _config["SendGrid:FromName"] ?? "Ventixe";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
                throw new Exception("SendGrid config saknas i Key Vault");

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var toAddress = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Body.ReadAsStringAsync();
                Console.WriteLine($"❌ Email sending failed: {response.StatusCode} - {error}");
                throw new Exception($"Email sending failed: {error}");
            }

            Console.WriteLine($"✅ Email sent to {to} - subject: {subject}");
        }
    }
}
