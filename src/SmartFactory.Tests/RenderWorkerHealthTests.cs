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

    [Fact]
    public void ListenerSocketCancellationIsExpectedDuringShutdown()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.True(RenderHealthEndpointService.IsExpectedListenerShutdown(
            new System.Net.Sockets.SocketException(125), cancellation.Token));
        Assert.True(RenderHealthEndpointService.IsExpectedListenerShutdown(
            new OperationCanceledException(), cancellation.Token));
        Assert.False(RenderHealthEndpointService.IsExpectedListenerShutdown(
            new System.Net.Sockets.SocketException(125), CancellationToken.None));
    }
}
