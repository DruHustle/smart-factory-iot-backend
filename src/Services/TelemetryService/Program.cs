using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.TelemetryService;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;
using SmartFactory.Services.TelemetryService.Infrastructure.Services;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Implementations;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Get connection string from environment variables (configured in Azure/local.settings.json)
        var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<TelemetryDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else
        {
            // Fallback for development/testing if connection string is missing
            services.AddDbContext<TelemetryDbContext>(options =>
                options.UseInMemoryDatabase("TelemetryDb"));
        }

        var eventBusConnection = Environment.GetEnvironmentVariable("RabbitMqConnectionString")
            ?? "amqp://guest:guest@localhost:5672";
        services.AddSingleton<IEventBus>(sp => new RabbitMqEventBus(eventBusConnection, sp));

        services.AddTransient<TelemetryFunction>();
        services.AddHostedService<MqttTelemetrySubscriberService>();
    })
    .Build();

host.Run();
