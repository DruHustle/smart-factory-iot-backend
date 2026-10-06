using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace SmartFactory.Services.DeviceService.Application.Services;

/// <summary>Publishes durable AAS mutations as transient cross-replica UI notifications.</summary>
public sealed class RedisAasChangePublisher(IConfiguration configuration, IHostEnvironment environment, ILogger<RedisAasChangePublisher> logger) : IHostedService, IAsyncDisposable
{
    public const string Channel = "smart-factory:websocket:aas:all";
    private readonly string? redisUrl = configuration["REDIS_URL"];
    private IConnectionMultiplexer? connection;

    public bool IsReady => string.IsNullOrWhiteSpace(redisUrl)
        ? !environment.IsProduction()
        : connection?.IsConnected == true;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(redisUrl))
        {
            if (environment.IsProduction()) throw new InvalidOperationException("REDIS_URL is required in production for cross-replica AAS notifications.");
            logger.LogInformation("REDIS_URL is unset; AAS change notifications are disabled in this development process.");
            return;
        }

        if (environment.IsProduction())
        {
            var redisUri = new Uri(redisUrl, UriKind.Absolute);
            if (redisUri.Scheme != "rediss" || string.IsNullOrWhiteSpace(redisUri.UserInfo))
                throw new InvalidOperationException("Production REDIS_URL must use rediss:// and include the managed Redis credentials.");
        }

        try
        {
            connection = await ConnectionMultiplexer.ConnectAsync(ParseRedisUrl(redisUrl));
        }
        catch (Exception error) when (!environment.IsProduction())
        {
            logger.LogWarning(error, "Redis is unavailable; AAS changes will persist but live notifications are disabled.");
        }
    }

    public async Task PublishAsync(string assetId, string change, int? version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetId) || assetId.Length > 128 || string.IsNullOrWhiteSpace(change) || change.Length > 32) return;
        var activeConnection = connection;
        if (activeConnection is null || !activeConnection.IsConnected) return;

        var envelope = SerializeEnvelope(assetId, change, version, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        try
        {
            await activeConnection.GetSubscriber().PublishAsync(RedisChannel.Literal(Channel), envelope);
        }
        catch (Exception error)
        {
            // Redis Pub/Sub is an ephemeral notification path. The repository
            // remains authoritative, so notification failures do not roll back AAS data.
            logger.LogWarning(error, "Could not publish AAS change event for {AssetId}.", assetId);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (connection is not null) await connection.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null) await connection.DisposeAsync();
    }

    public static string SerializeEnvelope(string assetId, string change, int? version, long timestamp) => JsonSerializer.Serialize(new
    {
        version = 1,
        channel = "aas:all",
        message = new
        {
            type = "aas_changed",
            data = new { assetId, change, version },
            timestamp,
        },
    });

    private static ConfigurationOptions ParseRedisUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("redis" or "rediss") || string.IsNullOrWhiteSpace(uri.Host) || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new InvalidOperationException("REDIS_URL must be a redis:// or rediss:// URL with an optional database number.");

        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = true,
            Ssl = uri.Scheme == "rediss",
            ConnectTimeout = 5_000,
            SyncTimeout = 5_000,
            AsyncTimeout = 5_000,
        };
        options.EndPoints.Add(uri.Host, uri.IsDefaultPort ? 6379 : uri.Port);

        var credentials = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        if (credentials.Length == 2)
        {
            if (credentials[0].Length > 0) options.User = credentials[0];
            if (credentials[1].Length > 0) options.Password = credentials[1];
        }
        else if (credentials.Length == 1 && credentials[0].Length > 0)
        {
            options.Password = credentials[0];
        }

        var database = uri.AbsolutePath.Trim('/');
        if (database.Length > 0)
        {
            if (!int.TryParse(database, NumberStyles.None, CultureInfo.InvariantCulture, out var databaseNumber) || databaseNumber < 0)
                throw new InvalidOperationException("REDIS_URL database path must be a non-negative integer.");
            options.DefaultDatabase = databaseNumber;
        }
        return options;
    }
}
