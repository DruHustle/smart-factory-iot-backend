using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Implementations;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.Services.AnalyticsService.Application.IntegrationEvents;
using SmartFactory.Services.AnalyticsService.Application.Services;

namespace SmartFactory.Services.AnalyticsService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // 1. Register Core Services
                    services.AddSingleton<AnalyticsEngine>();
                    
                    // 2. Register Event Bus
                    // In a real scenario, the connection string would come from configuration
                    var connectionString = hostContext.Configuration["RabbitMqConnectionString"] ?? "amqp://guest:guest@localhost:5672";
                    services.AddSingleton<IEventBus>(sp => new RabbitMqEventBus(connectionString, sp));

                    // 3. Register Integration Event Handlers
                    services.AddTransient<TelemetryReceivedIntegrationEventHandler>();

                    // 4. Register Background Service to manage subscriptions
                    services.AddHostedService<EventBusSubscriptionService>();
                    services.AddHostedService<RenderHealthEndpointService>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    /// <summary>
    /// Background service responsible for subscribing to integration events on startup.
    /// This follows the SOLID principles by separating the subscription logic from the main application entry point.
    /// </summary>
    public class EventBusSubscriptionService : BackgroundService
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<EventBusSubscriptionService> _logger;

        public EventBusSubscriptionService(IEventBus eventBus, ILogger<EventBusSubscriptionService> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Analytics Service is subscribing to events...");

            // Subscribe to TelemetryReceivedIntegrationEvent
            // The RabbitMQ implementation will handle the consumer setup
            _eventBus.Subscribe<TelemetryReceivedIntegrationEvent, TelemetryReceivedIntegrationEventHandler>();

            _logger.LogInformation("Analytics Service successfully subscribed to TelemetryReceivedIntegrationEvent.");

            return Task.CompletedTask;
        }
    }

    public sealed class RenderHealthEndpointService : BackgroundService
    {
        private readonly ILogger<RenderHealthEndpointService> _logger;
        private TcpListener? _listener;

        public RenderHealthEndpointService(ILogger<RenderHealthEndpointService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var port = ResolvePort();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _logger.LogInformation("Health endpoint listening on port {Port}.", port);

            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = Task.Run(async () =>
                {
                    await using var stream = client.GetStream();
                    var payload = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
                    await stream.WriteAsync(payload, stoppingToken);
                    client.Close();
                }, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _listener?.Stop();
            return base.StopAsync(cancellationToken);
        }

        private static int ResolvePort()
        {
            var fromPort = Environment.GetEnvironmentVariable("PORT");
            if (int.TryParse(fromPort, out var explicitPort))
            {
                return explicitPort;
            }

            var aspnetCoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (!string.IsNullOrWhiteSpace(aspnetCoreUrls))
            {
                foreach (var candidate in aspnetCoreUrls.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.Port > 0)
                    {
                        return uri.Port;
                    }
                }
            }

            return 10000;
        }
    }
}
