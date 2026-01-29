using System;

namespace SmartFactory.BuildingBlocks.EventBus.Models
{
    /// <summary>
    /// Integration event published when an anomaly is detected in telemetry data.
    /// </summary>
    public record AnomalyDetectedIntegrationEvent : IntegrationEvent
    {
        public string DeviceId { get; init; }
        public string AnomalyType { get; init; }
        public double Value { get; init; }
        public DateTime Timestamp { get; init; }

        public AnomalyDetectedIntegrationEvent(string deviceId, string anomalyType, double value, DateTime timestamp)
        {
            DeviceId = deviceId;
            AnomalyType = anomalyType;
            Value = value;
            Timestamp = timestamp;
        }
    }
}
