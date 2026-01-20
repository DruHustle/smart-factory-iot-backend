using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.DeviceService.Domain.Interfaces;
using SmartFactory.Services.DeviceService.Infrastructure.Data;
using SmartFactory.Services.DeviceService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext
builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseInMemoryDatabase("DeviceDb"));

// Register Repository
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

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
