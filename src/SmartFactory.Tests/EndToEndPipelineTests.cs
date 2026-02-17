using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.Services.AnalyticsService.Application.IntegrationEvents;
using SmartFactory.Services.AnalyticsService.Application.Services;
using SmartFactory.Services.NotificationService.Application.DTOs;
using SmartFactory.Services.NotificationService.Application.IntegrationEvents;
using SmartFactory.Services.NotificationService.Application.Interfaces;
using SmartFactory.Services.TelemetryService;
using SmartFactory.Services.TelemetryService.Application.DTOs;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;
using System.Text.Json;

namespace SmartFactory.Tests
{
    public class EndToEndPipelineTests
    {
        [Fact]
        public async Task Pipeline_ShouldPersistTelemetry_AndSendNotification_WhenAnomalyDetected()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TelemetryDbContext>(options =>
                options.UseInMemoryDatabase($"TelemetryE2E-{Guid.NewGuid():N}"));
            services.AddSingleton<AnalyticsEngine>();
            services.AddSingleton<TestNotificationService>();
            services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<TestNotificationService>());
            services.AddSingleton<IEventBus, InProcessEventBus>();
            services.AddTransient<TelemetryFunction>();
            services.AddTransient<TelemetryReceivedIntegrationEventHandler>();
            services.AddTransient<AnomalyDetectedIntegrationEventHandler>();

            using var provider = services.BuildServiceProvider();
            var eventBus = provider.GetRequiredService<IEventBus>();
            eventBus.Subscribe<TelemetryReceivedIntegrationEvent, TelemetryReceivedIntegrationEventHandler>();
            eventBus.Subscribe<AnomalyDetectedIntegrationEvent, AnomalyDetectedIntegrationEventHandler>();

            var telemetryFunction = provider.GetRequiredService<TelemetryFunction>();
            var message = JsonSerializer.Serialize(new TelemetryData
            {
                DeviceId = "Device-E2E-01",
                Temperature = 90.0,
                Humidity = 50.0,
                Vibration = 1.0,
                Timestamp = DateTime.UtcNow
            });

            var result = await telemetryFunction.Run(message);

            var db = provider.GetRequiredService<TelemetryDbContext>();
            var saved = await db.TelemetryRecords.SingleOrDefaultAsync(x => x.DeviceId == "Device-E2E-01");
            var notifier = provider.GetRequiredService<TestNotificationService>();

            Assert.NotNull(result);
            Assert.NotNull(saved);
            Assert.Equal("newTelemetry", result.Target);
            Assert.Single(notifier.LogicAppPayloads);
            Assert.Single(notifier.Emails);
            Assert.Contains("HighTemperature", notifier.LogicAppPayloads[0].AlertMessage);
        }

        private sealed class TestNotificationService : INotificationService
        {
            public List<EmailRequest> Emails { get; } = new();
            public List<AlertPayload> LogicAppPayloads { get; } = new();

            public Task SendEmailAsync(EmailRequest request)
            {
                Emails.Add(request);
                return Task.CompletedTask;
            }

            public Task TriggerLogicAppAsync(AlertPayload payload)
            {
                LogicAppPayloads.Add(payload);
                return Task.CompletedTask;
            }
        }

        private sealed class InProcessEventBus : IEventBus
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly Dictionary<Type, List<Type>> _subscriptions = new();

            public InProcessEventBus(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public Task PublishAsync(IntegrationEvent @event)
            {
                if (!_subscriptions.TryGetValue(@event.GetType(), out var handlers))
                {
                    return Task.CompletedTask;
                }

                return Dispatch(@event, handlers);
            }

            public void Subscribe<T, TH>()
                where T : IntegrationEvent
                where TH : IIntegrationEventHandler<T>
            {
                var key = typeof(T);
                if (!_subscriptions.TryGetValue(key, out var handlers))
                {
                    handlers = new List<Type>();
                    _subscriptions[key] = handlers;
                }

                handlers.Add(typeof(TH));
            }

            private async Task Dispatch(IntegrationEvent @event, List<Type> handlers)
            {
                using var scope = _serviceProvider.CreateScope();
                foreach (var handlerType in handlers)
                {
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    var handleMethod = handlerType.GetMethod("Handle");
                    if (handleMethod == null)
                    {
                        continue;
                    }

                    var task = (Task?)handleMethod.Invoke(handler, new object[] { @event });
                    if (task != null)
                    {
                        await task;
                    }
                }
            }
        }
    }
}
