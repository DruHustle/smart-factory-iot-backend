using SmartFactory.BuildingBlocks.EventBus.Models;

namespace SmartFactory.BuildingBlocks.EventBus.Models
{
    public record TelemetryReceivedIntegrationEvent : IntegrationEvent
    {
        public string DeviceId { get; init; } = string.Empty;
        public double Temperature { get; init; }
        public double Humidity { get; init; }
        public double Vibration { get; init; }
        public DateTime Timestamp { get; init; }

        public TelemetryReceivedIntegrationEvent(string deviceId, double temperature, double humidity, double vibration, DateTime timestamp)
        {
            DeviceId = deviceId;
            Temperature = temperature;
            Humidity = humidity;
            Vibration = vibration;
            Timestamp = timestamp;
        }
    }
}
