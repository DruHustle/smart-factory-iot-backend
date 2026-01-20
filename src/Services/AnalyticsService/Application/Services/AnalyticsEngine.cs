using SmartFactory.Services.AnalyticsService.Domain.Models;

namespace SmartFactory.Services.AnalyticsService.Application.Services
{
    public class AnalyticsEngine
    {
        public double CalculateOEE(double availability, double performance, double quality)
        {
            return availability * performance * quality;
        }

        public IndustrialMetrics ProcessSensorData(double temperature, double humidity, double vibration)
        {
            return new IndustrialMetrics
            {
                IsAnomaly = vibration > 5.0 || temperature > 80.0,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
