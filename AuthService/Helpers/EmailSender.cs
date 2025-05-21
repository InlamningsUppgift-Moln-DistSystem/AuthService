namespace AuthService.Helpers
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            Console.WriteLine($"Email sent to {to} with subject '{subject}' and body: {body}");
            return Task.CompletedTask;
        }
    }
}
