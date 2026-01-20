using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
        public async Task Run([EventHubTrigger("messages/events", Connection = "IoTHubConnectionString", IsBatched = false)] string message, FunctionContext context)
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

                    _logger.LogInformation($"Successfully saved telemetry for device {data.DeviceId} to database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing telemetry: {ex.Message}");
            }
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
}
