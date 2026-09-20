using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Abstractions;
using Microsoft.Extensions.Logging;
using NModbus;
using DomainProtocolType = IIoT.FaultDiagnosis.Domain.Enums.ProtocolType;

namespace IIoT.FaultDiagnosis.Protocols.Modbus;

public sealed class ModbusProtocolAdapter(ILogger<ModbusProtocolAdapter> logger) : IModbusProtocolAdapter
{
    private readonly ConcurrentDictionary<Guid, TcpClient> _connections = new();
    private readonly IModbusFactory _factory = new ModbusFactory();

    public DomainProtocolType ProtocolType => DomainProtocolType.ModbusTcp;

    public async Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);

        var client = new TcpClient
        {
            ReceiveTimeout = 1000,
            SendTimeout = 1000
        };
        try
        {
            await client.ConnectAsync(device.Host, device.Port, cancellationToken);
            if (_connections.TryGetValue(device.Id, out var previous))
            {
                previous.Dispose();
            }

            _connections[device.Id] = client;
            return ConnectionResult.Connected();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            client.Dispose();
            throw;
        }
        catch (Exception exception)
        {
            client.Dispose();
            var fault = ModbusFaultNormalizer.Normalize(exception);
            logger.LogWarning(
                exception,
                "Modbus connection failed. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ErrorCode={ErrorCode}",
                device.Id,
                ProtocolType,
                fault.FaultType,
                fault.ErrorCode);
            return ConnectionResult.Failed(fault.FaultType, fault.ErrorCode, exception.Message);
        }
    }

    public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken) =>
        CollectAsync(device, ModbusConfiguration.Default, cancellationToken);

    public async Task<CollectionResult> CollectAsync(
        Device device,
        ModbusConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        var rawRequest = $"Function=03;SlaveId={configuration.SlaveId};StartAddress={configuration.StartAddress};Length={configuration.NumberOfPoints}";
        if (!_connections.TryGetValue(device.Id, out var client) || !client.Connected)
        {
            return CollectionResult.Failed(FaultType.ConnectionLost, "NotConnected", "No active Modbus TCP connection exists for this device.", rawRequest);
        }

        try
        {
            var master = _factory.CreateMaster(client);
            var readTask = master.ReadHoldingRegistersAsync(
                configuration.SlaveId,
                configuration.StartAddress,
                configuration.NumberOfPoints);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            if (await Task.WhenAny(readTask, timeoutTask) != readTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return CollectionResult.Failed(FaultType.Timeout, "ReadTimeout", "Modbus read did not complete within one second.", rawRequest);
            }

            var registers = await readTask;
            var payload = JsonSerializer.Serialize(registers);
            return new CollectionResult(true, FaultType.None, null, null, rawRequest, payload, payload);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var fault = ModbusFaultNormalizer.Normalize(exception);
            logger.LogWarning(
                exception,
                "Modbus collection failed. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ErrorCode={ErrorCode}",
                device.Id,
                ProtocolType,
                fault.FaultType,
                fault.ErrorCode);
            return CollectionResult.Failed(fault.FaultType, fault.ErrorCode, exception.Message, rawRequest);
        }
    }

    public Task DisconnectAsync(Device device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(device);

        if (_connections.TryRemove(device.Id, out var client))
        {
            client.Dispose();
        }

        return Task.CompletedTask;
    }
}
