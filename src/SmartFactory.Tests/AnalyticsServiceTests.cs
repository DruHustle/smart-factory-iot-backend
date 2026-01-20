using Xunit;
using SmartFactory.Services.AnalyticsService.Application.Services;

namespace SmartFactory.Tests
{
    public class AnalyticsServiceTests
    {
        [Fact]
        public void CalculateOEE_ShouldReturnCorrectValue()
        {
            // Arrange
            var engine = new AnalyticsEngine();
            double availability = 0.9;
            double performance = 0.8;
            double quality = 0.95;
            double expected = 0.9 * 0.8 * 0.95;

            // Act
            var result = engine.CalculateOEE(availability, performance, quality);

            // Assert
            Assert.Equal(expected, result, 5);
        }

        [Fact]
        public void ProcessSensorData_ShouldDetectAnomaly_WhenTemperatureHigh()
        {
            // Arrange
            var engine = new AnalyticsEngine();
            double temperature = 85.0;
            double humidity = 50.0;
            double vibration = 1.0;

            // Act
            var result = engine.ProcessSensorData(temperature, humidity, vibration);

            // Assert
            Assert.True(result.IsAnomaly);
        }

        [Fact]
        public void ProcessSensorData_ShouldDetectAnomaly_WhenVibrationHigh()
        {
            // Arrange
            var engine = new AnalyticsEngine();
            double temperature = 25.0;
            double humidity = 50.0;
            double vibration = 6.0;

            // Act
            var result = engine.ProcessSensorData(temperature, humidity, vibration);

            // Assert
            Assert.True(result.IsAnomaly);
        }

        [Fact]
        public void ProcessSensorData_ShouldNotDetectAnomaly_WhenValuesNormal()
        {
            // Arrange
            var engine = new AnalyticsEngine();
            double temperature = 25.0;
            double humidity = 50.0;
            double vibration = 1.0;

            // Act
            var result = engine.ProcessSensorData(temperature, humidity, vibration);

            // Assert
            Assert.False(result.IsAnomaly);
        }
    }
}
