using System.Text.Json;
using SmartFactory.Services.DeviceService.Application.Services;

namespace SmartFactory.Tests;

public sealed class RedisAasChangePublisherTests
{
    [Fact]
    public void EmitsTheDashboardRedisFanoutEnvelope()
    {
        using var document = JsonDocument.Parse(RedisAasChangePublisher.SerializeEnvelope("urn:factory:compressor:01", "updated", 7, 1_800_000_000_000));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("aas:all", root.GetProperty("channel").GetString());
        Assert.Equal("aas_changed", root.GetProperty("message").GetProperty("type").GetString());
        Assert.Equal("urn:factory:compressor:01", root.GetProperty("message").GetProperty("data").GetProperty("assetId").GetString());
        Assert.Equal(7, root.GetProperty("message").GetProperty("data").GetProperty("version").GetInt32());
        Assert.Equal(1_800_000_000_000, root.GetProperty("message").GetProperty("timestamp").GetInt64());
    }
}
