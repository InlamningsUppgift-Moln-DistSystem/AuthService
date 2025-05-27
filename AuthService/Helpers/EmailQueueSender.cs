using Azure.Messaging.ServiceBus;
using AuthService.DTOs;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AuthService.Helpers
{
    public class EmailQueueSender
    {
        private readonly string _connectionString;

        public EmailQueueSender(IConfiguration config)
        {
            _connectionString = config["ServiceBus:ConnectionString"]!;
        }

        public async Task SendEmailAsync(EmailMessageDto message)
        {
            await using var client = new ServiceBusClient(_connectionString);
            var sender = client.CreateSender("email-queue"); // <-- hårdkodad queue

            var json = JsonSerializer.Serialize(message);
            var busMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            await sender.SendMessageAsync(busMessage);

            Console.WriteLine($"📤 Email queued to {message.To}");
        }
    }
}
