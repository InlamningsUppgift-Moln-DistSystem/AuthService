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
        public async Task<IActionResult> Get()
        {
            var result = new Dictionary<string, string>();

            // 🔍 1. Databasanslutning
            try
            {
                using var conn = new SqlConnection(_config["DefaultConnection"]);
                await conn.OpenAsync();
                result["Database"] = "✅ OK";
            }
            catch (Exception ex)
            {
                result["Database"] = $"❌ {ex.Message}";
            }

            // 🔍 2. Service Bus
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

            // 🔍 3. Key Vault kontroll
            result["KeyVaultUrl"] = _config["KeyVaultUrl"] ?? "❌ MISSING";

            return Ok(result);
        }
    }
}
