using System.Text.Json;
using SmartFactory.Services.DeviceService.Application.Services;

namespace SmartFactory.Tests;

public sealed class EdgeConfigurationWireTests
{
    [Fact]
    public void ConfigurationWireFormatMatchesPythonGatewayContract()
    {
        var request = new EdgeConfigurationRequest(1, "pi-edge-01", new()
        {
            new("urn:test:motor", "Motor", "modbus_tcp", "192.0.2.10:502", new()),
        });
        using var parsed = JsonDocument.Parse(EdgeConfigurationPublisher.BuildConfigurationCommand(request));
        var command = parsed.RootElement;
        Assert.Equal("replace_asset_configuration", command.GetProperty("action").GetString());
        var configuration = command.GetProperty("configuration");
        Assert.Equal(1, configuration.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("pi-edge-01", configuration.GetProperty("gatewayDeviceId").GetString());
        Assert.Equal("modbus_tcp", configuration.GetProperty("assets")[0].GetProperty("protocol").GetString());
        Assert.False(configuration.TryGetProperty("SchemaVersion", out _));
    }

    [Theory]
    [InlineData("base", "increase")]
    [InlineData("shoulder", "decrease")]
    [InlineData("elbow", "increase")]
    [InlineData("wrist_rotation", "decrease")]
    [InlineData("gripper", "increase")]
    public void Ada031V2JogWireFormatMatchesGatewayContract(string joint, string direction)
    {
        using var parsed = JsonDocument.Parse(EdgeConfigurationPublisher.BuildAda031ControlCommand(Request(
            action: "jog", joint: joint, direction: direction)));

        var command = parsed.RootElement;
        Assert.Equal(2, command.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("jog", command.GetProperty("action").GetString());
        Assert.Equal("command-0001", command.GetProperty("commandId").GetString());
        Assert.Equal("urn:test:arm", command.GetProperty("targetAssetId").GetString());
        Assert.Equal(joint, command.GetProperty("joint").GetString());
        Assert.Equal(direction, command.GetProperty("direction").GetString());
        Assert.False(command.TryGetProperty("profile", out _));
        Assert.Equal(7, command.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("pick_and_place_repeat")]
    [InlineData("demonstration_moves")]
    public void Ada031V2ProfileWireFormatMatchesGatewayContract(string profile)
    {
        using var parsed = JsonDocument.Parse(EdgeConfigurationPublisher.BuildAda031ControlCommand(Request(
            action: "set_profile", profile: profile)));

        var command = parsed.RootElement;
        Assert.Equal("set_profile", command.GetProperty("action").GetString());
        Assert.Equal(profile, command.GetProperty("profile").GetString());
        Assert.False(command.TryGetProperty("joint", out _));
        Assert.False(command.TryGetProperty("direction", out _));
        Assert.Equal(6, command.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("neutral")]
    [InlineData("stop_program")]
    public void Ada031V2ProgramControlWireFormatContainsOnlyCommonFields(string action)
    {
        using var parsed = JsonDocument.Parse(EdgeConfigurationPublisher.BuildAda031ControlCommand(Request(action)));

        var command = parsed.RootElement;
        Assert.Equal(action, command.GetProperty("action").GetString());
        Assert.False(command.TryGetProperty("joint", out _));
        Assert.False(command.TryGetProperty("direction", out _));
        Assert.False(command.TryGetProperty("profile", out _));
        Assert.Equal(5, command.EnumerateObject().Count());
    }

    [Fact]
    public void Ada031V1JogRemainsCompatibleDuringRollingDeployment()
    {
        var request = Request(action: null, joint: "base", direction: "increase") with { SchemaVersion = 1 };
        using var parsed = JsonDocument.Parse(EdgeConfigurationPublisher.BuildAda031ControlCommand(request));

        Assert.Equal(1, parsed.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("ada031_control", parsed.RootElement.GetProperty("action").GetString());
    }

    [Theory]
    [InlineData("jog", null, null, null)]
    [InlineData("jog", "base", "increase", "pick_and_place_repeat")]
    [InlineData("set_profile", "base", "increase", null)]
    [InlineData("set_profile", null, null, "unknown")]
    [InlineData("neutral", "base", "increase", null)]
    [InlineData("unknown", null, null, null)]
    public void Ada031V2RejectsAmbiguousOrUnsupportedShapes(string action, string? joint, string? direction, string? profile)
    {
        Assert.Throws<ArgumentException>(() => EdgeConfigurationPublisher.BuildAda031ControlCommand(
            Request(action, joint, direction, profile)));
    }

    private static Ada031ControlRequest Request(
        string? action,
        string? joint = null,
        string? direction = null,
        string? profile = null) => new()
        {
            SchemaVersion = 2,
            GatewayDeviceId = "pi-edge-01",
            AssetId = "urn:test:arm",
            Action = action,
            Joint = joint,
            Direction = direction,
            Profile = profile,
            CommandId = "command-0001",
            ExpiresAt = 1_900_000_000_000,
        };
}
