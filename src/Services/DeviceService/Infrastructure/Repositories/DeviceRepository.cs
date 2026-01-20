using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.DeviceService.Domain.Entities;
using SmartFactory.Services.DeviceService.Domain.Interfaces;
using SmartFactory.Services.DeviceService.Infrastructure.Data;

namespace SmartFactory.Services.DeviceService.Infrastructure.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly DeviceDbContext _context;

        public DeviceRepository(DeviceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Device>> GetAllAsync()
        {
            return await _context.Devices.ToListAsync();
        }

        public async Task<Device> GetByIdAsync(int id)
        {
            return await _context.Devices.FindAsync(id);
        }

        public async Task AddAsync(Device device)
        {
            await _context.Devices.AddAsync(device);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Device device)
        {
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
            }
        }
    }
}
