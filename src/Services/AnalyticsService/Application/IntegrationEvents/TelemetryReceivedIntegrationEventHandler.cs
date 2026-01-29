using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.Services.AnalyticsService.Application.Services;
using Microsoft.Extensions.Logging;

namespace SmartFactory.Services.AnalyticsService.Application.IntegrationEvents
{
    /// <summary>
    /// Handles the TelemetryReceivedIntegrationEvent by processing data through the AnalyticsEngine
    /// and publishing an AnomalyDetectedIntegrationEvent if an anomaly is found.
    /// This follows the Single Responsibility Principle by focusing only on the orchestration of telemetry analysis.
    /// </summary>
    public class TelemetryReceivedIntegrationEventHandler : IIntegrationEventHandler<TelemetryReceivedIntegrationEvent>
    {
        private readonly AnalyticsEngine _analyticsEngine;
        private readonly ILogger<TelemetryReceivedIntegrationEventHandler> _logger;
        private readonly IEventBus _eventBus;

        public TelemetryReceivedIntegrationEventHandler(
            AnalyticsEngine analyticsEngine,
            ILogger<TelemetryReceivedIntegrationEventHandler> logger,
            IEventBus eventBus)
        {
            _analyticsEngine = analyticsEngine;
            _logger = logger;
            _eventBus = eventBus;
        }

        public async Task Handle(TelemetryReceivedIntegrationEvent @event)
        {
            _logger.LogInformation($"Handling telemetry for device: {@event.DeviceId}");

            var metrics = _analyticsEngine.ProcessSensorData(@event.Temperature, @event.Humidity, @event.Vibration);

            if (metrics.IsAnomaly)
            {
                _logger.LogWarning($"Anomaly detected for device: {@event.DeviceId}!");
                
                string anomalyType = @event.Vibration > 5.0 ? "HighVibration" : "HighTemperature";
                double value = @event.Vibration > 5.0 ? @event.Vibration : @event.Temperature;

                var anomalyEvent = new AnomalyDetectedIntegrationEvent(
                    @event.DeviceId,
                    anomalyType,
                    value,
                    @event.Timestamp
                );

                await _eventBus.PublishAsync(anomalyEvent);
            }
        }
    }
}
