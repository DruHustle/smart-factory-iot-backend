namespace SmartFactory.Services.NotificationService.Application.DTOs
{
    public class AlertPayload
    {
        public string AlertName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
