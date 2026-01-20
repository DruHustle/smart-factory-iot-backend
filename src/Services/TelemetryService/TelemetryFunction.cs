using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace SmartFactory.Services.TelemetryService
{
    /// <summary>
    /// Handles telemetry data processing, historical data retrieval, and real-time broadcasting.
    /// </summary>
    public class TelemetryFunction
    {
        private readonly ILogger _logger;
        private readonly TelemetryDbContext _dbContext;

        public TelemetryFunction(ILoggerFactory loggerFactory, TelemetryDbContext dbContext)
        {
            _logger = loggerFactory.CreateLogger<TelemetryFunction>();
            _dbContext = dbContext;
        }

        /// <summary>
        /// Triggered by Azure IoT Hub (via Event Hubs) when new telemetry data arrives.
        /// Parses the JSON payload, saves it to the database, and broadcasts it via SignalR.
        /// </summary>
        /// <param name="message">The raw JSON message from the IoT device.</param>
        /// <param name="context">The function execution context.</param>
        /// <returns>A SignalRMessageAction to broadcast the data to connected clients.</returns>
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
                // PropertyNameCaseInsensitive is used to handle variations in JSON key casing.
                var data = JsonSerializer.Deserialize<TelemetryData>(message, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (data != null)
                {
                    // 2. Map the DTO to a Database Entity and Save to MySQL
                    var record = new TelemetryRecord
                    {
                        DeviceId = data.DeviceId,
                        Temperature = data.Temperature,
                        Humidity = data.Humidity,
                        Vibration = data.Vibration,
                        Timestamp = data.Timestamp ?? DateTime.UtcNow // Use current time if device didn't provide one
                    };

                    _dbContext.TelemetryRecords.Add(record);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Successfully saved telemetry for device {data.DeviceId}.");

                    // 3. Return a SignalR message action. 
                    // This automatically broadcasts the new record to all clients listening for "newTelemetry".
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

        /// <summary>
        /// HTTP GET endpoint to retrieve the last 100 telemetry records for a specific device.
        /// Used by the React dashboard to populate historical charts.
        /// </summary>
        /// <param name="req">The HTTP request object.</param>
        /// <param name="deviceId">The unique identifier of the device.</param>
        /// <param name="executionContext">The function execution context.</param>
        /// <returns>An HTTP response containing the list of telemetry records in JSON format.</returns>
        [Function("GetHistoricalTelemetry")]
        public async Task<HttpResponseData> GetHistoricalTelemetry(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "telemetry/{deviceId}")] HttpRequestData req,
            string deviceId,
            FunctionContext executionContext)
        {
            _logger.LogInformation($"Fetching historical telemetry for device: {deviceId}");

            // Query the database for the most recent 100 records for the given device.
            var records = await _dbContext.TelemetryRecords
                .Where(r => r.DeviceId == deviceId)
                .OrderByDescending(r => r.Timestamp)
                .Take(100)
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(records);
            return response;
        }

        /// <summary>
        /// Required endpoint for SignalR clients to negotiate a connection.
        /// Returns the connection information (URL and Access Token) for the Azure SignalR Service.
        /// </summary>
        /// <param name="req">The HTTP request object.</param>
        /// <param name="connectionInfo">The connection info provided by the SignalR input binding.</param>
        /// <returns>An HTTP response containing the SignalR connection information.</returns>
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

    /// <summary>
    /// Data Transfer Object (DTO) representing the telemetry payload sent by IoT devices.
    /// </summary>
    public class TelemetryData
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Database entity representing a single telemetry record stored in MySQL.
    /// </summary>
    public class TelemetryRecord
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Entity Framework Core database context for telemetry data.
    /// </summary>
    public class TelemetryDbContext : DbContext
    {
        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }
        public DbSet<TelemetryRecord> TelemetryRecords { get; set; }
    }

    /// <summary>
    /// Represents a message to be sent via SignalR.
    /// </summary>
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
