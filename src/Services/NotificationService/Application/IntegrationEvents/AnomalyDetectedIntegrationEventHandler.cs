using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.Services.NotificationService.Application.Interfaces;
using SmartFactory.Services.NotificationService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace SmartFactory.Services.NotificationService.Application.IntegrationEvents
{
    /// <summary>
    /// Integration event handler that processes anomaly detection events from the Analytics Service.
    /// This class follows the Single Responsibility Principle by focusing solely on handling the integration event.
    /// </summary>
    public class AnomalyDetectedIntegrationEventHandler : IIntegrationEventHandler<AnomalyDetectedIntegrationEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<AnomalyDetectedIntegrationEventHandler> _logger;

        public AnomalyDetectedIntegrationEventHandler(
            INotificationService notificationService,
            ILogger<AnomalyDetectedIntegrationEventHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Handles the AnomalyDetectedIntegrationEvent by triggering appropriate notifications.
        /// </summary>
        /// <param name="event">The anomaly detection event data.</param>
        public async Task Handle(AnomalyDetectedIntegrationEvent @event)
        {
            _logger.LogInformation($"Processing anomaly notification for device: {@event.DeviceId}");

            // 1. Prepare alert payload for Logic App or other downstream systems
            var alertPayload = new AlertPayload
            {
                DeviceId = @event.DeviceId,
                AlertMessage = $"CRITICAL: {@event.AnomalyType} detected! Value: {@event.Value}",
                Severity = "Critical",
                Timestamp = @event.Timestamp
            };

            // 2. Trigger Logic App (as per existing NotificationService capability)
            await _notificationService.TriggerLogicAppAsync(alertPayload);

            // 3. Send Email Notification
            var emailRequest = new EmailRequest
            {
                To = "factory-admin@smartfactory.com",
                Subject = $"[ALERT] Anomaly Detected in Device {@event.DeviceId}",
                Body = $"<h1>Anomaly Alert</h1>" +
                       $"<p>A critical anomaly has been detected in the smart factory system.</p>" +
                       $"<ul>" +
                       $"<li><strong>Device ID:</strong> {@event.DeviceId}</li>" +
                       $"<li><strong>Anomaly Type:</strong> {@event.AnomalyType}</li>" +
                       $"<li><strong>Recorded Value:</strong> {@event.Value}</li>" +
                       $"<li><strong>Timestamp:</strong> {@event.Timestamp:yyyy-MM-dd HH:mm:ss}</li>" +
                       $"</ul>" +
                       $"<p>Please investigate immediately.</p>"
            };

            try 
            {
                await _notificationService.SendEmailAsync(emailRequest);
                _logger.LogInformation($"Notification email sent for device: {@event.DeviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification email for device: {@event.DeviceId}");
            }
        }
    }


}
