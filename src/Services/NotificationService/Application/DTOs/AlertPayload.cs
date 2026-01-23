using System;

namespace SmartFactory.Services.NotificationService.Application.DTOs
{
    /// <summary>
    /// Represents the data transfer object for alert notifications.
    /// This class follows the Single Responsibility Principle by serving only as a data carrier
    /// for alert-related information across system boundaries.
    /// </summary>
    public class AlertPayload
    {
        /// <summary>
        /// Gets or sets the unique identifier of the device that triggered the alert.
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the descriptive message detailing the nature of the alert.
        /// </summary>
        public string AlertMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity level of the alert (e.g., Information, Warning, Critical).
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the alert event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the name of the alert for categorization purposes.
        /// Note: Kept for backward compatibility or future use if needed.
        /// </summary>
        public string AlertName { get; set; } = string.Empty;
    }
}
