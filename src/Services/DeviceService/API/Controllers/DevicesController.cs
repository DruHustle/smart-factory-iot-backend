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
                Status = d.Status
            });
            return Ok(deviceDtos);
        }

        [HttpPost]
        public async Task<ActionResult<DeviceDto>> RegisterDevice(DeviceDto deviceDto)
        {
            var device = new Device
            {
                DeviceId = deviceDto.DeviceId,
                Name = deviceDto.Name,
                Type = deviceDto.Type,
                Status = deviceDto.Status
            };

            await _repository.AddAsync(device);
            deviceDto.Id = device.Id;

            return CreatedAtAction(nameof(GetDevices), new { id = device.Id }, deviceDto);
        }
    }
}
