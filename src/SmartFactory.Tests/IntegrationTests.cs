using Microsoft.Extensions.Logging;
using SmartFactory.Services.TelemetryService;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;
using SmartFactory.Services.TelemetryService.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SmartFactory.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task TelemetryFunction_ShouldPersistNormalizedData_WhenDataReceived()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TelemetryDbContext>()
                .UseInMemoryDatabase(databaseName: "TelemetryTestDb")
                .Options;

            using var dbContext = new TelemetryDbContext(options);
            var loggerFactory = new LoggerFactory();
            var function = new TelemetryFunction(loggerFactory, dbContext);

            var telemetryData = new TelemetryData
            {
                DeviceId = "Device001",
                Temperature = 25.5,
                Humidity = 60.0,
                Vibration = 1.2,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            var message = JsonSerializer.Serialize(telemetryData);

            // Act
            await function.Run(message);
            var persisted = Assert.Single(dbContext.TelemetryRecords);
            Assert.Equal("Device001", persisted.DeviceId);
            Assert.Equal(25.5, persisted.Temperature);
            Assert.Equal(60.0, persisted.Humidity);
            Assert.Equal(1.2, persisted.Vibration);
        }

    }
}
