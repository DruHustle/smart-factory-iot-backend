using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SmartFactory.Services.TelemetryService;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.Services.TelemetryService.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SmartFactory.Services.AnalyticsService.Application.IntegrationEvents;
using SmartFactory.Services.AnalyticsService.Application.Services;

namespace SmartFactory.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task TelemetryFunction_ShouldPublishEvent_WhenDataReceived()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TelemetryDbContext>()
                .UseInMemoryDatabase(databaseName: "TelemetryTestDb")
                .Options;

            using var dbContext = new TelemetryDbContext(options);
            var loggerFactory = new LoggerFactory();
            var mockEventBus = new Mock<IEventBus>();
            
            var function = new TelemetryFunction(loggerFactory, dbContext, mockEventBus.Object);

            var telemetryData = new TelemetryData
            {
                DeviceId = "Device001",
                Temperature = 25.5,
                Humidity = 60.0,
                Vibration = 1.2,
                Timestamp = DateTime.UtcNow
            };
            var message = JsonSerializer.Serialize(telemetryData);

            // Act
            await function.Run(message, null!);

            // Assert
            mockEventBus.Verify(x => x.PublishAsync(It.Is<TelemetryReceivedIntegrationEvent>(e => 
                e.DeviceId == "Device001" && 
                e.Temperature == 25.5)), Times.Once);
        }

        [Fact]
        public async Task TelemetryHandler_ShouldPublishAnomalyEvent_WhenAnomalyDetected()
        {
            // Arrange
            var mockEventBus = new Mock<IEventBus>();
            var mockLogger = new Mock<ILogger<TelemetryReceivedIntegrationEventHandler>>();
            var analyticsEngine = new AnalyticsEngine();
            var handler = new TelemetryReceivedIntegrationEventHandler(analyticsEngine, mockLogger.Object, mockEventBus.Object);

            var telemetryEvent = new TelemetryReceivedIntegrationEvent(
                "Device001",
                90.0, // High temperature
                50.0,
                1.0,
                DateTime.UtcNow
            );

            // Act
            await handler.Handle(telemetryEvent);

            // Assert
            mockEventBus.Verify(x => x.PublishAsync(It.Is<AnomalyDetectedIntegrationEvent>(e => 
                e.DeviceId == "Device001" && 
                e.AnomalyType == "HighTemperature")), Times.Once);
        }
    }
}
