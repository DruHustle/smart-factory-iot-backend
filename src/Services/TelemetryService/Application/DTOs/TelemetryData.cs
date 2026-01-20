namespace SmartFactory.Services.TelemetryService.Application.DTOs
{
    public class TelemetryData
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
