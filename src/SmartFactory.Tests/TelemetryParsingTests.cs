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
            const long timestamp = 1791045000000;
            var json = "{\"deviceId\":\"esp32-wrover-01\",\"gatewayId\":\"pi-edge-01\",\"assetId\":\"urn:test:compressor\",\"temperature\":25.5,\"humidity\":60.2,\"vibration\":0.5,\"power\":120.0,\"pressure\":7.2,\"rpm\":1450,\"timestamp\":" + timestamp + "}";

            // Act
            var data = JsonSerializer.Deserialize<TelemetryData>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            // Assert
            Assert.NotNull(data);
            Assert.Equal("esp32-wrover-01", data.DeviceId);
            Assert.Equal("pi-edge-01", data.GatewayId);
            Assert.Equal("urn:test:compressor", data.AssetId);
            Assert.Equal(25.5, data.Temperature);
            Assert.Equal(60.2, data.Humidity);
            Assert.Equal(0.5, data.Vibration);
            Assert.Equal(120.0, data.Power);
            Assert.Equal(7.2, data.Pressure);
            Assert.Equal(1450.0, data.Rpm);
            Assert.Equal(timestamp, data.Timestamp);
        }

        [Fact]
        public void Should_Handle_Missing_Timestamp()
        {
            // Arrange
            var json = "{\"deviceId\":\"esp32-wrover-01\",\"temperature\":22.1}";

            // Act
            var data = JsonSerializer.Deserialize<TelemetryData>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            // Assert
            Assert.NotNull(data);
            Assert.Equal("esp32-wrover-01", data.DeviceId);
            Assert.Null(data.Timestamp);
            Assert.Null(data.Humidity);
        }

        [Fact]
        public void Should_Parse_Telemetry_Without_Gateway_For_Legacy_Devices()
        {
            var data = JsonSerializer.Deserialize<TelemetryData>("{\"deviceId\":\"standalone-sensor\"}", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(data);
            Assert.Null(data.GatewayId);
        }
    }
}
