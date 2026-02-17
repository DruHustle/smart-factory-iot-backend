using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace SmartFactory.Services.TelemetryService.Infrastructure.Services
{
    public class MqttTelemetrySubscriberService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttTelemetrySubscriberService> _logger;
        private readonly IMqttClient _mqttClient;
        private readonly string _host;
        private readonly int _port;
        private readonly string _topic;
        private readonly string? _username;
        private readonly string? _password;

        public MqttTelemetrySubscriberService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<MqttTelemetrySubscriberService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _mqttClient = new MqttFactory().CreateMqttClient();
            _host = configuration["Mqtt:Host"] ?? Environment.GetEnvironmentVariable("MqttBrokerHost") ?? "localhost";
            _port = int.TryParse(configuration["Mqtt:Port"] ?? Environment.GetEnvironmentVariable("MqttBrokerPort"), out var configuredPort)
                ? configuredPort
                : 1883;
            _topic = configuration["Mqtt:Topic"] ?? Environment.GetEnvironmentVariable("MqttTopic") ?? "smartfactory/telemetry";
            _username = configuration["Mqtt:Username"] ?? Environment.GetEnvironmentVariable("MqttUsername");
            _password = configuration["Mqtt:Password"] ?? Environment.GetEnvironmentVariable("MqttPassword");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = e.ApplicationMessage?.PayloadSegment.Array;
                if (payload == null)
                {
                    return;
                }

                var message = System.Text.Encoding.UTF8.GetString(payload, e.ApplicationMessage!.PayloadSegment.Offset, e.ApplicationMessage.PayloadSegment.Count);
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var telemetryProcessor = scope.ServiceProvider.GetRequiredService<TelemetryFunction>();
                    await telemetryProcessor.Run(message, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process MQTT telemetry message.");
                }
            };

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithCleanSession();

            if (!string.IsNullOrWhiteSpace(_username))
            {
                optionsBuilder.WithCredentials(_username, _password);
            }

            try
            {
                await _mqttClient.ConnectAsync(optionsBuilder.Build(), stoppingToken);
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_topic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(), stoppingToken);
                _logger.LogInformation("Connected to MQTT broker at {Host}:{Port} and subscribed to topic {Topic}.", _host, _port, _topic);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to MQTT broker. Telemetry MQTT ingestion is disabled.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
