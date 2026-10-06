using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartFactory.Services.TelemetryService.Infrastructure.Services;

/// <summary>Single-consumer persistent delivery queue; unfinished records survive process restart.</summary>
public sealed class TelemetryOutboxService(IServiceScopeFactory scopes, ILogger<TelemetryOutboxService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<TelemetryFunction>().DrainPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Telemetry outbox is unavailable; pending deliveries remain in PostgreSQL."); }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
