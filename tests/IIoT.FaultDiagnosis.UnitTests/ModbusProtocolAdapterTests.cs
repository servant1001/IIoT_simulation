using System.Net;
using System.Net.Sockets;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class ModbusProtocolAdapterTests
{
    [Fact]
    public async Task CollectAsync_ReadsHoldingRegistersFromModbusTcpServer()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = RespondToHoldingRegisterReadAsync(listener, CancellationToken.None);
        var device = new Device(
            Guid.NewGuid(),
            "Test Modbus",
            IIoT.FaultDiagnosis.Domain.Enums.ProtocolType.ModbusTcp,
            "127.0.0.1",
            port,
            true,
            DateTimeOffset.UtcNow);
        var adapter = new ModbusProtocolAdapter(NullLogger<ModbusProtocolAdapter>.Instance);

        var connected = await adapter.ConnectAsync(device, CancellationToken.None);
        var result = await adapter.CollectAsync(device, new ModbusConfiguration(1, 0, 2), CancellationToken.None);
        await adapter.DisconnectAsync(device, CancellationToken.None);
        await serverTask;

        Assert.True(connected.Success);
        Assert.True(result.Success);
        Assert.Equal(FaultType.None, result.FaultType);
        Assert.Equal("[42,43]", result.ParsedValue);
        Assert.Contains("Function=03", result.RawRequest, StringComparison.Ordinal);
    }

    [Fact]
    public void Normalize_MapsSocketConnectionRefused()
    {
        var fault = ModbusFaultNormalizer.Normalize(new SocketException((int)SocketError.ConnectionRefused));

        Assert.Equal(FaultType.ConnectionRefused, fault.FaultType);
        Assert.Equal("ConnectionRefused", fault.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(126)]
    public void ModbusConfiguration_RejectsInvalidRegisterCount(int numberOfPoints)
    {
        var configuration = new ModbusConfiguration(1, 0, (ushort)numberOfPoints);

        Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);
    }

    private static async Task RespondToHoldingRegisterReadAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        using var stream = client.GetStream();
        var request = new byte[12];
        await stream.ReadExactlyAsync(request, cancellationToken);

        Assert.Equal((byte)3, request[7]);
        var response = new byte[]
        {
            request[0], request[1], 0, 0, 0, 7, request[6], 3, 4, 0, 42, 0, 43
        };
        await stream.WriteAsync(response, cancellationToken);
    }
}
