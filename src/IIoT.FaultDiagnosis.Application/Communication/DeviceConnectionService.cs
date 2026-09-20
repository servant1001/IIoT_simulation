using System.Diagnostics;
using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Abstractions;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using IIoT.FaultDiagnosis.Protocols.Mqtt;
using IIoT.FaultDiagnosis.Protocols.OpcUa;
using Microsoft.Extensions.Logging;

namespace IIoT.FaultDiagnosis.Application.Communication;

public sealed class DeviceConnectionService(
    IDeviceRepository deviceRepository,
    ICommunicationRecordRepository communicationRecordRepository,
    IModbusProtocolAdapter modbusAdapter,
    IOpcUaProtocolAdapter opcUaAdapter,
    IMqttProtocolAdapter mqttAdapter,
    TimeProvider timeProvider,
    ILogger<DeviceConnectionService> logger) : IDeviceConnectionService
{
    public async Task<DeviceConnectionTestResult?> TestModbusAsync(
        Guid deviceId,
        ModbusConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        return await TestAsync(
            deviceId,
            ProtocolType.ModbusTcp,
            modbusAdapter,
            (device, token) => modbusAdapter.CollectAsync(device, configuration, token),
            BuildRawRequest(configuration),
            null,
            cancellationToken);
    }

    public async Task<DeviceConnectionTestResult?> TestOpcUaAsync(
        Guid deviceId,
        OpcUaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        return await TestAsync(
            deviceId,
            ProtocolType.OpcUa,
            opcUaAdapter,
            (device, token) => opcUaAdapter.CollectAsync(device, configuration, token),
            $"NodeId={configuration.NodeIds[0]}",
            (device, token) => opcUaAdapter.ConnectAsync(device, configuration, token),
            cancellationToken);
    }

    public async Task<DeviceConnectionTestResult?> TestMqttAsync(
        Guid deviceId,
        MqttConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        return await TestAsync(
            deviceId,
            ProtocolType.Mqtt,
            mqttAdapter,
            (device, token) => mqttAdapter.CollectAsync(device, configuration, token),
            $"Topic={configuration.Topic};QoS={configuration.QualityOfService}",
            (device, token) => mqttAdapter.ConnectAsync(device, configuration, token),
            cancellationToken);
    }

    private async Task<DeviceConnectionTestResult?> TestAsync(
        Guid deviceId,
        ProtocolType expectedProtocol,
        IProtocolAdapter adapter,
        Func<Device, CancellationToken, Task<CollectionResult>> collectAsync,
        string rawRequest,
        Func<Device, CancellationToken, Task<ConnectionResult>>? connectAsync,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        var startedAt = timeProvider.GetUtcNow();
        var stopwatch = Stopwatch.StartNew();
        CollectionResult collectionResult;
        try
        {
            if (device.ProtocolType != expectedProtocol)
            {
                collectionResult = CollectionResult.Failed(
                    FaultType.Unknown,
                    "ProtocolMismatch",
                    $"This endpoint requires a {expectedProtocol} device.",
                    rawRequest);
            }
            else
            {
                var connectionResult = connectAsync is null
                    ? await adapter.ConnectAsync(device, cancellationToken)
                    : await connectAsync(device, cancellationToken);
                collectionResult = connectionResult.Success
                    ? await collectAsync(device, cancellationToken)
                    : CollectionResult.Failed(
                        connectionResult.FaultType,
                        connectionResult.ErrorCode,
                        connectionResult.ErrorMessage,
                        rawRequest);
            }
        }
        finally
        {
            if (device.ProtocolType == expectedProtocol)
            {
                await adapter.DisconnectAsync(device, CancellationToken.None);
            }
        }

        stopwatch.Stop();
        var completedAt = timeProvider.GetUtcNow();
        var status = collectionResult.Success
            ? CommunicationStatus.Success
            : collectionResult.FaultType == FaultType.Timeout
                ? CommunicationStatus.Timeout
                : CommunicationStatus.Failed;
        var record = new CommunicationRecord(
            Guid.NewGuid(),
            device.Id,
            device.ProtocolType,
            startedAt,
            completedAt,
            stopwatch.Elapsed.TotalMilliseconds,
            status,
            collectionResult.FaultType,
            collectionResult.ErrorCode,
            collectionResult.ErrorMessage,
            collectionResult.RawRequest,
            collectionResult.RawResponse,
            collectionResult.ParsedValue,
            completedAt);

        await communicationRecordRepository.AddAsync(record, cancellationToken);
        await communicationRecordRepository.SaveChangesAsync(cancellationToken);

        if (collectionResult.Success)
        {
            logger.LogInformation(
                "Device connection test succeeded. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ResponseTimeMs={ResponseTimeMs}",
                device.Id,
                device.ProtocolType,
                collectionResult.FaultType,
                stopwatch.Elapsed.TotalMilliseconds);
        }
        else
        {
            logger.LogWarning(
                "Device connection test completed with failure. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ResponseTimeMs={ResponseTimeMs}, ErrorCode={ErrorCode}",
                device.Id,
                device.ProtocolType,
                collectionResult.FaultType,
                stopwatch.Elapsed.TotalMilliseconds,
                collectionResult.ErrorCode);
        }

        return new DeviceConnectionTestResult(
            record.Id,
            collectionResult.Success,
            stopwatch.Elapsed.TotalMilliseconds,
            collectionResult.FaultType,
            collectionResult.ErrorCode,
            collectionResult.Success ? "Connection successful" : collectionResult.ErrorMessage,
            collectionResult.ParsedValue);
    }

    private static string BuildRawRequest(ModbusConfiguration configuration) =>
        $"Function=03;SlaveId={configuration.SlaveId};StartAddress={configuration.StartAddress};Length={configuration.NumberOfPoints}";
}
