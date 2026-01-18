namespace SmartFactory.Services.AnalyticsService
{
    public class AnalyticsEngine
    {
        /// <summary>
        /// Calculates Overall Equipment Effectiveness (OEE)
        /// OEE = Availability x Performance x Quality
        /// </summary>
        public double CalculateOEE(double availability, double performance, double quality)
        {
            return availability * performance * quality;
        }

        public IndustrialMetrics ProcessSensorData(double temperature, double humidity, double vibration)
        {
            // Logic to detect anomalies in vibration or temperature
            return new IndustrialMetrics
            {
                IsAnomaly = vibration > 5.0 || temperature > 80.0,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public class IndustrialMetrics
    {
        public bool IsAnomaly { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
