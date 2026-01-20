using System;
using System.Text.Json;
using Xunit;
using SmartFactory.Services.TelemetryService.Application.DTOs;

namespace SmartFactory.Tests
{
    public class TelemetryParsingTests
    {
        [Fact]
        public void Should_Parse_Valid_Telemetry_Json()
        {
            // Arrange
            var json = "{\"DeviceId\": \"RPi-001\", \"Temperature\": 25.5, \"Humidity\": 60.2, \"Vibration\": 0.5, \"Timestamp\": \"2026-01-20T10:00:00Z\"}";

            // Act
            var data = JsonSerializer.Deserialize<TelemetryData>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            // Assert
            Assert.NotNull(data);
            Assert.Equal("RPi-001", data.DeviceId);
            Assert.Equal(25.5, data.Temperature);
            Assert.Equal(60.2, data.Humidity);
            Assert.Equal(0.5, data.Vibration);
            Assert.Equal(DateTime.Parse("2026-01-20T10:00:00Z").ToUniversalTime(), data.Timestamp?.ToUniversalTime());
        }

        [Fact]
        public void Should_Handle_Missing_Timestamp()
        {
            // Arrange
            var json = "{\"DeviceId\": \"ESP32-01\", \"Temperature\": 22.1, \"Humidity\": 45.0, \"Vibration\": 0.1}";

            // Act
            var data = JsonSerializer.Deserialize<TelemetryData>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            // Assert
            Assert.NotNull(data);
            Assert.Equal("ESP32-01", data.DeviceId);
            Assert.Null(data.Timestamp);
        }
    }
}
