namespace SmartFactory.Services.DeviceService.Application.DTOs
{
    public class UpdateRequestDto
    {
        public string TargetVersion { get; set; } = string.Empty;
        public string UpdatePackageUrl { get; set; } = string.Empty;
    }
}
