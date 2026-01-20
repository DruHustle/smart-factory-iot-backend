namespace SmartFactory.Services.DeviceService.Application.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
