using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace SmartFactory.Services.TelemetryService
{
    public class TelemetryFunction
    {
        private readonly ILogger _logger;
        private readonly TelemetryDbContext _dbContext;

        public TelemetryFunction(ILoggerFactory loggerFactory, TelemetryDbContext dbContext)
        {
            _logger = loggerFactory.CreateLogger<TelemetryFunction>();
            _dbContext = dbContext;
        }

        [Function("ProcessTelemetry")]
        [SignalROutput(HubName = "telemetryHub", ConnectionStringSetting = "AzureSignalRConnectionString")]
        public async Task<SignalRMessageAction> Run(
            [EventHubTrigger("messages/events", Connection = "IoTHubConnectionString", IsBatched = false)] string message,
            FunctionContext context)
        {
            _logger.LogInformation($"C# IoT Hub trigger function processed a message: {message}");

            try
            {
                // 1. Parse the JSON telemetry from ESP-32/Raspberry Pi
                var data = JsonSerializer.Deserialize<TelemetryData>(message, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (data != null)
                {
                    // 2. Map to Entity and Save to MySQL
                    var record = new TelemetryRecord
                    {
                        DeviceId = data.DeviceId,
                        Temperature = data.Temperature,
                        Humidity = data.Humidity,
                        Vibration = data.Vibration,
                        Timestamp = data.Timestamp ?? DateTime.UtcNow
                    };

                    _dbContext.TelemetryRecords.Add(record);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Successfully saved telemetry for device {data.DeviceId}.");

                    // 3. Return SignalR message for real-time dashboard updates
                    return new SignalRMessageAction("newTelemetry")
                    {
                        Arguments = new[] { record }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing telemetry: {ex.Message}");
            }

            return null!;
        }

        [Function("GetHistoricalTelemetry")]
        public async Task<HttpResponseData> GetHistoricalTelemetry(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "telemetry/{deviceId}")] HttpRequestData req,
            string deviceId,
            FunctionContext executionContext)
        {
            _logger.LogInformation($"Fetching historical telemetry for device: {deviceId}");

            var records = await _dbContext.TelemetryRecords
                .Where(r => r.DeviceId == deviceId)
                .OrderByDescending(r => r.Timestamp)
                .Take(100)
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(records);
            return response;
        }

        [Function("negotiate")]
        public static HttpResponseData Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "telemetryHub", ConnectionStringSetting = "AzureSignalRConnectionString")] string connectionInfo)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(connectionInfo);
            return response;
        }
    }

    // DTO for incoming JSON
    public class TelemetryData
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    // Entity for Database
    public class TelemetryRecord
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Database Context
    public class TelemetryDbContext : DbContext
    {
        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }
        public DbSet<TelemetryRecord> TelemetryRecords { get; set; }
    }

    public class SignalRMessageAction
    {
        public SignalRMessageAction(string target)
        {
            Target = target;
        }

        public string Target { get; set; }
        public object[] Arguments { get; set; }
    }
}
