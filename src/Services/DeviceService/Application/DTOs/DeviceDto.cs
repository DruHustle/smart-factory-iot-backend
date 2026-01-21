using System;

namespace SmartFactory.Services.DeviceService.Application.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        
        // OTA/FOTA Fields
        public string FirmwareVersion { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;
        public DateTime? LastUpdateDate { get; set; }
        public string? PendingUpdateVersion { get; set; }
        public string? UpdateStatus { get; set; }
    }
}
