using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
}
