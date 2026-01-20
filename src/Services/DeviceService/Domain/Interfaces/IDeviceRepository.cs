using SmartFactory.Services.DeviceService.Domain.Entities;

namespace SmartFactory.Services.DeviceService.Domain.Interfaces
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAllAsync();
        Task<Device> GetByIdAsync(int id);
        Task AddAsync(Device device);
        Task UpdateAsync(Device device);
        Task DeleteAsync(int id);
    }
}
