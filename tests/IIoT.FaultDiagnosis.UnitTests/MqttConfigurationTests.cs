using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Mqtt;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class MqttConfigurationTests
{
    [Theory]
    [InlineData("factory/+/data")]
    [InlineData("factory/#")]
    [InlineData("")]
    public void Validate_RejectsWildcardOrEmptyTopics(string topic) =>
        Assert.Throws<ArgumentException>(() => new MqttConfiguration(topic).Validate());

    [Fact]
    public void Validate_RejectsAnUnsupportedQos() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new MqttConfiguration("factory/machine01/data", QualityOfService: 3).Validate());

    [Theory]
    [InlineData("NotAuthorized", FaultType.AuthenticationFailed)]
    [InlineData("BadUserNameOrPassword", FaultType.AuthenticationFailed)]
    [InlineData("Invalid topic filter", FaultType.TopicMismatch)]
    [InlineData("MQTT client disconnected", FaultType.ConnectionLost)]
    [InlineData("Unable to connect to broker", FaultType.BrokerUnavailable)]
    public void Normalize_MapsKnownMqttFailures(string message, FaultType expected) =>
        Assert.Equal(expected, MqttFaultNormalizer.Normalize(new InvalidOperationException(message)));

    [Fact]
    public void Normalize_MapsJsonErrorsToPayloadInvalid() =>
        Assert.Equal(FaultType.PayloadInvalid, MqttFaultNormalizer.Normalize(new System.Text.Json.JsonException("Invalid JSON")));

    [Fact]
    public async Task ConnectAsync_MapsAnUnavailableBroker()
    {
        var adapter = new MqttProtocolAdapter(NullLogger<MqttProtocolAdapter>.Instance);
        var device = new Device(
            Guid.NewGuid(),
            "Unavailable MQTT broker",
            ProtocolType.Mqtt,
            "127.0.0.1",
            GetClosedLoopbackPort(),
            true,
            DateTimeOffset.UtcNow);

        var result = await adapter.ConnectAsync(device, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(FaultType.BrokerUnavailable, result.FaultType);
    }

    private static int GetClosedLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}
