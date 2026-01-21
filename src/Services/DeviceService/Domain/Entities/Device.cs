using System;

namespace SmartFactory.Services.DeviceService.Domain.Entities
{
    public class Device
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        
        // OTA/FOTA Fields
        public string FirmwareVersion { get; set; } = "1.0.0";
        public string SoftwareVersion { get; set; } = "1.0.0";
        public DateTime? LastUpdateDate { get; set; }
        public string? PendingUpdateVersion { get; set; }
        public string? UpdateStatus { get; set; } // e.g., Idle, Downloading, Installing, Completed, Failed
    }
}
