using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Implementations;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Get connection string from environment variables (configured in Azure/local.settings.json)
        var connectionString = Environment.GetEnvironmentVariable("MySqlConnectionString");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<TelemetryDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        }
        else
        {
            // Fallback for development/testing if connection string is missing
            services.AddDbContext<TelemetryDbContext>(options =>
                options.UseInMemoryDatabase("TelemetryDb"));
        }

        var eventBusConnection = Environment.GetEnvironmentVariable("EventBusConnectionString");
        if (!string.IsNullOrEmpty(eventBusConnection))
        {
            services.AddSingleton<IEventBus>(new AzureServiceBus(eventBusConnection));
        }
        else
        {
            // For testing/development, we could add a mock or local implementation
            // For now, let's assume it's required or provided via config
        }
    })
    .Build();

host.Run();
