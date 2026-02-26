using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SmartFactory.Services.TelemetryService;
using SmartFactory.Services.TelemetryService.Infrastructure.Data;
using SmartFactory.Services.TelemetryService.Infrastructure.Services;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Implementations;

var host = Host.CreateDefaultBuilder(args)
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
        services.AddHostedService<RenderHealthEndpointService>();
    })
    .Build();

host.Run();

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
