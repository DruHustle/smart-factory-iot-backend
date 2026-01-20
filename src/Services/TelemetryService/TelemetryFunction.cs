using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartFactory.Services.TelemetryService.Application.DTOs;
using SmartFactory.Services.TelemetryService.Domain.Entities;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;

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
                var data = JsonSerializer.Deserialize<TelemetryData>(message, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (data != null)
                {
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

    public class SignalRMessageAction
    {
        public SignalRMessageAction(string target)
        {
            Target = target;
        }
        public string Target { get; set; }
        public object[] Arguments { get; set; } = Array.Empty<object>();
    }
}
