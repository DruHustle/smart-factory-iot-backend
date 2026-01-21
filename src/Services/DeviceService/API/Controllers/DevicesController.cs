using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> TriggerUpdate(int id, [FromBody] UpdateRequestDto updateRequest)
        {
            var device = await _repository.GetByIdAsync(id);
            if (device == null) return NotFound();

            // In a real scenario, this would publish an event to a message bus (RabbitMQ/Azure Service Bus)
            // or notify the device via MQTT/SignalR.
            device.PendingUpdateVersion = updateRequest.TargetVersion;
            device.UpdateStatus = "Pending";
            
            await _repository.UpdateAsync(device);

            return Accepted(new { Message = $"Update to {updateRequest.TargetVersion} triggered for device {device.DeviceId}" });
        }

        [HttpPost("{id}/update-status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var device = await _repository.GetByIdAsync(id);
            if (device == null) return NotFound();

            device.UpdateStatus = status;
            if (status == "Completed")
            {
                device.FirmwareVersion = device.PendingUpdateVersion ?? device.FirmwareVersion;
                device.PendingUpdateVersion = null;
                device.LastUpdateDate = DateTime.UtcNow;
            }

            await _repository.UpdateAsync(device);
            return Ok();
        }
    }
}
