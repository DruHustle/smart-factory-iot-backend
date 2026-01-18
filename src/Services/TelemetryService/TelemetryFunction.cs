using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SmartFactory.Services.TelemetryService
{
    public class TelemetryFunction
    {
        private readonly ILogger _logger;

        public TelemetryFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TelemetryFunction>();
        }

        [Function("ProcessTelemetry")]
        public void Run([EventHubTrigger("messages/events", Connection = "IoTHubConnectionString")] string message, FunctionContext context)
        {
            _logger.LogInformation($"C# IoT Hub trigger function processed a message: {message}");
            
            // Logic to parse ESP-32 sensor data (Temp, Humidity, Vibration)
            // Logic to save to MySQL
            // Logic to push to SignalR for real-time dashboard
        }
    }
}
