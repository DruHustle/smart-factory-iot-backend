using System.Net;

namespace SmartFactory.Tests;

public sealed class RenderWorkerHealthTests
{
    [Theory]
    [InlineData("worker", "render-bundle", true)]
    [InlineData("WORKER", "render-bundle", true)]
    [InlineData("all", "render-bundle", false)]
    [InlineData("web", "render-bundle", false)]
    [InlineData(null, "local", true)]
    public void TelemetryHealthEndpointUsesExpectedInterface(string? role, string? mode, bool publicInterface)
    {
        var address = RenderHealthEndpointService.ResolveBindAddress(role, mode);
        Assert.Equal(publicInterface ? IPAddress.Any : IPAddress.Loopback, address);
    }
}
