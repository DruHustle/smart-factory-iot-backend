using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.DeviceService.Domain.Interfaces;
using SmartFactory.Services.DeviceService.Infrastructure.Data;
using SmartFactory.Services.DeviceService.Infrastructure.Repositories;
using SmartFactory.Services.DeviceService.Infrastructure.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext with PostgreSQL support for production/local dev
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<DeviceDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback for isolated unit testing or rapid development
    builder.Services.AddDbContext<DeviceDbContext>(options =>
        options.UseInMemoryDatabase("DeviceDb"));
}

// Register Repository (Dependency Inversion Principle)
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// Register Background Service for OTA Updates (Single Responsibility Principle)
builder.Services.AddHostedService<UpdateManagerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
