using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartFactory.Services.DeviceService.Domain.Entities;
using SmartFactory.Services.DeviceService.Domain.Interfaces;
using SmartFactory.Services.DeviceService.Application.DTOs;

namespace SmartFactory.Services.DeviceService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceRepository _repository;

        public DevicesController(IDeviceRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Authorize(Roles = "user,viewer,operator,engineer,admin")]
        public async Task<ActionResult<IEnumerable<DeviceDto>>> GetDevices()
        {
            var devices = await _repository.GetAllAsync();
            var deviceDtos = devices.Select(d => new DeviceDto
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                Name = d.Name,
                Type = d.Type,
                Status = d.Status,
                FirmwareVersion = d.FirmwareVersion,
                SoftwareVersion = d.SoftwareVersion,
                LastUpdateDate = d.LastUpdateDate,
                PendingUpdateVersion = d.PendingUpdateVersion,
                UpdateStatus = d.UpdateStatus
            });
            return Ok(deviceDtos);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "user,viewer,operator,engineer,admin")]
        public async Task<ActionResult<DeviceDto>> GetDevice(int id)
        {
            var d = await _repository.GetByIdAsync(id);
            if (d == null) return NotFound();

            return Ok(new DeviceDto
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                Name = d.Name,
                Type = d.Type,
                Status = d.Status,
                FirmwareVersion = d.FirmwareVersion,
                SoftwareVersion = d.SoftwareVersion,
                LastUpdateDate = d.LastUpdateDate,
                PendingUpdateVersion = d.PendingUpdateVersion,
                UpdateStatus = d.UpdateStatus
            });
        }

        [HttpPost]
        [Authorize(Roles = "engineer,admin")]
        public async Task<ActionResult<DeviceDto>> RegisterDevice(DeviceDto deviceDto)
        {
            var device = new Device
            {
                DeviceId = deviceDto.DeviceId,
                Name = deviceDto.Name,
                Type = deviceDto.Type,
                Status = deviceDto.Status,
                FirmwareVersion = string.IsNullOrEmpty(deviceDto.FirmwareVersion) ? "1.0.0" : deviceDto.FirmwareVersion,
                SoftwareVersion = string.IsNullOrEmpty(deviceDto.SoftwareVersion) ? "1.0.0" : deviceDto.SoftwareVersion,
                UpdateStatus = "Idle"
            };

            await _repository.AddAsync(device);
            deviceDto.Id = device.Id;

            return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, deviceDto);
        }

        [HttpPost("{id}/trigger-update")]
        [Authorize(Roles = "engineer,admin")]
        public IActionResult TriggerUpdate(int id, [FromBody] UpdateRequestDto updateRequest)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                error = "OTA delivery is not configured. No firmware update was queued.",
            });
        }

        [HttpPost("{id}/update-status")]
        [Authorize(Roles = "engineer,admin")]
        public IActionResult UpdateStatus(int id, [FromBody] string status)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                error = "Device-reported OTA status is not configured. No firmware status was changed.",
            });
        }
    }
}
