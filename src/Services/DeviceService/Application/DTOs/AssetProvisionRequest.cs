using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Services.DeviceService.Application.DTOs;

public sealed record AssetProvisionRequest
{
    [Required, StringLength(128, MinimumLength = 3)] public string AssetId { get; init; } = "";
    [Required, StringLength(255, MinimumLength = 2)] public string Name { get; init; } = "";
    [Required, RegularExpression("^(compressor|transformer|pump|motor|wind_turbine|robotic_arm|other)$")] public string AssetType { get; init; } = "";
    [Required, StringLength(255)] public string Manufacturer { get; init; } = "";
    [Required, StringLength(255)] public string Model { get; init; } = "";
    [Required, StringLength(255)] public string ManufacturerStreet { get; init; } = "";
    [Required, StringLength(32)] public string ManufacturerZipcode { get; init; } = "";
    [Required, StringLength(255)] public string ManufacturerCityTown { get; init; } = "";
    [Required, RegularExpression("^[A-Za-z]{2}$")] public string ManufacturerNationalCode { get; init; } = "";
    [Required, StringLength(128)] public string ManufacturerArticleNumber { get; init; } = "";
    [Required, StringLength(128)] public string OrderCodeOfManufacturer { get; init; } = "";
    [StringLength(128)] public string? SerialNumber { get; init; }
    [StringLength(80)] public string? RatedValue { get; init; }
    [StringLength(32)] public string? RatedUnit { get; init; }
    [Range(1, int.MaxValue)] public int AasVersion { get; init; } = 1;
    [Range(1, int.MaxValue)] public int? ExpectedAasVersion { get; init; }
}
