namespace SmartFactory.Services.TelemetryService.Domain.Entities
{
    public class TelemetryRecord
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
