using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.DeviceService.Domain.Entities;

namespace SmartFactory.Services.DeviceService.Infrastructure.Data
{
    public class DeviceDbContext : DbContext
    {
        public DeviceDbContext(DbContextOptions<DeviceDbContext> options) : base(options) { }
        public DbSet<Device> Devices { get; set; }
    }
}
