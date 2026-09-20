using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Protocols.OpcUa;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class OpcUaConfigurationTests
{
    [Fact]
    public void Validate_RejectsNonOpcTcpEndpoint() =>
        Assert.Throws<ArgumentException>(() => new OpcUaConfiguration("http://localhost", ["ns=2;s=X"]).Validate());

    [Fact]
    public void Validate_RejectsMissingNodeIds() =>
        Assert.Throws<ArgumentException>(() => new OpcUaConfiguration("opc.tcp://localhost:4840", []).Validate());

    [Fact]
    public void Validate_RejectsAnOperationTimeoutBelowOneHundredMilliseconds() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpcUaConfiguration("opc.tcp://localhost:4840", ["i=2258"], OperationTimeoutMs: 99).Validate());

    [Theory]
    [InlineData("BadNodeIdUnknown", FaultType.InvalidNodeId)]
    [InlineData("Certificate validation failed", FaultType.CertificateError)]
    [InlineData("BadSecurityChecksFailed", FaultType.CertificateError)]
    [InlineData("Session Closed by server", FaultType.SessionDisconnected)]
    [InlineData("BadSessionIdInvalid", FaultType.SessionDisconnected)]
    [InlineData("BadSecureChannelClosed", FaultType.SessionDisconnected)]
    [InlineData("BadConnectionClosed", FaultType.SessionDisconnected)]
    [InlineData("Endpoint could not be reached", FaultType.InvalidEndpoint)]
    public void Normalize_MapsKnownOpcUaFailures(string message, FaultType expected) =>
        Assert.Equal(expected, OpcUaFaultNormalizer.Normalize(new InvalidOperationException(message)));

    [Fact]
    public void Normalize_MapsTimeoutException() =>
        Assert.Equal(FaultType.Timeout, OpcUaFaultNormalizer.Normalize(new TimeoutException()));

    [Fact]
    public async Task ConnectAsync_MapsAnUnavailableEndpoint()
    {
        var adapter = new OpcUaProtocolAdapter();
        var device = new Device(
            Guid.NewGuid(),
            "Unavailable OPC UA server",
            ProtocolType.OpcUa,
            "127.0.0.1",
            GetClosedLoopbackPort(),
            true,
            DateTimeOffset.UtcNow);

        var result = await adapter.ConnectAsync(device, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(FaultType.InvalidEndpoint, result.FaultType);
    }

    private static int GetClosedLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}
