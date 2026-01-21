using SmartFactory.BuildingBlocks.EventBus.Models;

namespace SmartFactory.BuildingBlocks.EventBus.Models
{
    /// <summary>
    /// Integration event triggered when new telemetry data is received from a device.
    /// This event is typically published by the Telemetry Service and consumed by the Analytics or Notification services.
    /// </summary>
    public record TelemetryReceivedIntegrationEvent : IntegrationEvent
    {
        /// <summary>
        /// Gets the unique identifier of the device that sent the telemetry.
        /// </summary>
        public string DeviceId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the temperature reading from the device.
        /// </summary>
        public double Temperature { get; init; }

        /// <summary>
        /// Gets the humidity reading from the device.
        /// </summary>
        public double Humidity { get; init; }

        /// <summary>
        /// Gets the vibration reading from the device.
        /// </summary>
        public double Vibration { get; init; }

        /// <summary>
        /// Gets the timestamp when the telemetry was recorded by the device.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryReceivedIntegrationEvent"/> record.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="temperature">The temperature reading.</param>
        /// <param name="humidity">The humidity reading.</param>
        /// <param name="vibration">The vibration reading.</param>
        /// <param name="timestamp">The recording timestamp.</param>
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
