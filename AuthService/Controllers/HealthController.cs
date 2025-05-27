using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public HealthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            var result = new Dictionary<string, string>();

            // 1. DB-koll
            try
            {
                var conn = new SqlConnection(_config["DefaultConnection"]);
                await conn.OpenAsync();
                result["Database"] = "✅ OK";
                conn.Close();
            }
            catch (Exception ex)
            {
                result["Database"] = $"❌ {ex.Message}";
            }

            // 2. Service Bus-koll
            try
            {
                var client = new ServiceBusClient(_config["ServiceBus:ConnectionString"]);
                var sender = client.CreateSender(_config["ServiceBus:QueueName"]);
                result["ServiceBus"] = "✅ OK";
            }
            catch (Exception ex)
            {
                result["ServiceBus"] = $"❌ {ex.Message}";
            }

            return Ok(result);
        }
    }
}
