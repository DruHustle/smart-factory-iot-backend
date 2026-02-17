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
builder.Services.AddSingleton<IEventBus>(sp => new RabbitMqEventBus(connectionString, sp));

// Register Integration Event Handlers
builder.Services.AddTransient<AnomalyDetectedIntegrationEventHandler>();

var app = builder.Build();

// Subscribe to events
var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<AnomalyDetectedIntegrationEvent, AnomalyDetectedIntegrationEventHandler>();

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
