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
        private readonly string _queueName;

        public EmailQueueSender(IConfiguration config)
        {
            _connectionString = config["ServiceBus:ConnectionString"]!;
            _queueName = config["ServiceBus:QueueName"]!;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new EmailMessageDto { To = to, Subject = subject, Body = body };

            await using var client = new ServiceBusClient(_connectionString);
            var sender = client.CreateSender(_queueName);

            var json = JsonSerializer.Serialize(message);
            var busMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            await sender.SendMessageAsync(busMessage);
            Console.WriteLine($"📤 Email queued for {to}");
        }
    }
}
