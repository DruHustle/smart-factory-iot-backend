using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.TelemetryService.Domain.Entities;

namespace SmartFactory.Services.TelemetryService.Infrastructure.Data
{
    public class TelemetryDbContext : DbContext
    {
        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }
        public DbSet<TelemetryRecord> TelemetryRecords { get; set; }
    }
}
