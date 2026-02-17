using Microsoft.Identity.Web;
using SmartFactory.Services.NotificationService.Application.Interfaces;
using SmartFactory.Services.NotificationService.Infrastructure.Services;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Implementations;
using SmartFactory.BuildingBlocks.EventBus.Models;
using SmartFactory.Services.NotificationService.Application.IntegrationEvents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("GraphApi"))
    .AddInMemoryTokenCaches();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Services
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register Event Bus
var connectionString = builder.Configuration["RabbitMqConnectionString"] ?? "amqp://guest:guest@localhost:5672";
builder.Services.AddSingleton<IEventBus>(sp =>
{
    try
    {
        return new RabbitMqEventBus(connectionString, sp);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("NotificationService.EventBus");
        logger.LogError(ex, "Failed to connect to RabbitMQ during startup. Falling back to NoOp event bus.");
        return new NoOpEventBus(logger);
    }
});

// Register Integration Event Handlers
builder.Services.AddTransient<AnomalyDetectedIntegrationEventHandler>();

var app = builder.Build();

// Subscribe to events
try
{
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    eventBus.Subscribe<AnomalyDetectedIntegrationEvent, AnomalyDetectedIntegrationEventHandler>();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to subscribe to anomaly events. Service will continue without event subscriptions.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public sealed class NoOpEventBus : IEventBus
{
    private readonly ILogger _logger;

    public NoOpEventBus(ILogger logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IntegrationEvent @event)
    {
        _logger.LogWarning("NoOpEventBus active. Dropping event {EventType}.", @event.GetType().Name);
        return Task.CompletedTask;
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        _logger.LogWarning("NoOpEventBus active. Skipping subscription for {EventType}.", typeof(T).Name);
    }
}
