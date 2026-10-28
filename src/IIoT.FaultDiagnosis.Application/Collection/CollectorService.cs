using System.Diagnostics;
using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Abstractions;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using IIoT.FaultDiagnosis.Protocols.Mqtt;
using IIoT.FaultDiagnosis.Protocols.OpcUa;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IIoT.FaultDiagnosis.Application.Collection;

public sealed class CollectorService(
    IDeviceRepository deviceRepository,
    ICommunicationRecordRepository communicationRecordRepository,
    IModbusProtocolAdapter modbusAdapter,
    IOpcUaProtocolAdapter opcUaAdapter,
    IMqttProtocolAdapter mqttAdapter,
    IOptions<CollectorOptions> options,
    TimeProvider timeProvider,
    ILogger<CollectorService> logger) : ICollectorService
{
    public async Task<IReadOnlyList<CollectorDeviceResult>> CollectEnabledDevicesAsync(
        CollectorExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Validate();
        var collectorOptions = options.Value;
        collectorOptions.Validate();
        var devices = await deviceRepository.ListAsync(cancellationToken);
        var results = new List<CollectorDeviceResult>();

        foreach (var device in devices.Where(device => device.IsEnabled))
        {
            try
            {
                results.Add(await CollectDeviceAsync(device, context, collectorOptions, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Collector isolated an unexpected device failure. DeviceId={DeviceId}, Protocol={Protocol}",
                    device.Id,
                    device.ProtocolType);
            }
        }

        return results;
    }

    private async Task<CollectorDeviceResult> CollectDeviceAsync(
        Device device,
        CollectorExecutionContext context,
        CollectorOptions collectorOptions,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var stopwatch = Stopwatch.StartNew();
        var attempt = await CollectWithRetryAsync(device, collectorOptions, cancellationToken);

        stopwatch.Stop();
        var completedAt = timeProvider.GetUtcNow();
        var status = attempt.Result.Success
            ? CommunicationStatus.Success
            : attempt.Result.FaultType == FaultType.Timeout
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
            attempt.Result.FaultType,
            attempt.Result.ErrorCode,
            attempt.Result.ErrorMessage,
            attempt.Result.RawRequest,
            attempt.Result.RawResponse,
            attempt.Result.ParsedValue,
            completedAt,
            context.ExperimentId,
            context.ExperimentRunId,
            attempt.RetryCount);

        await communicationRecordRepository.AddAsync(record, cancellationToken);
        await communicationRecordRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Collector finished device collection. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ResponseTimeMs={ResponseTimeMs}, RetryCount={RetryCount}, ExperimentId={ExperimentId}, ExperimentRunId={ExperimentRunId}",
            device.Id,
            device.ProtocolType,
            attempt.Result.FaultType,
            stopwatch.Elapsed.TotalMilliseconds,
            attempt.RetryCount,
            context.ExperimentId,
            context.ExperimentRunId);

        return new CollectorDeviceResult(
            device.Id,
            record.Id,
            attempt.Result.Success,
            attempt.Result.FaultType,
            attempt.RetryCount,
            stopwatch.Elapsed.TotalMilliseconds);
    }

    private async Task<CollectionAttempt> CollectWithRetryAsync(Device device, CollectorOptions collectorOptions, CancellationToken cancellationToken)
    {
        var settings = DeviceProtocolSettings.Deserialize(device.ProtocolSettingsJson);
        settings.ValidateFor(device.ProtocolType);
        var (adapter, connectAsync, collectAsync) = BuildProtocolOperations(device, settings);
        CollectionResult? lastResult = null;
        for (var attempt = 0; attempt <= collectorOptions.MaxRetry; attempt++)
        {
            try
            {
                var connectionResult = await connectAsync(cancellationToken);
                lastResult = connectionResult.Success
                    ? await collectAsync(cancellationToken)
                    : CollectionResult.Failed(
                        connectionResult.FaultType,
                        connectionResult.ErrorCode,
                        connectionResult.ErrorMessage,
                        BuildRawRequest());
            }
            finally
            {
                await adapter.DisconnectAsync(device, CancellationToken.None);
            }

            if (lastResult.Success || attempt == collectorOptions.MaxRetry)
            {
                return new CollectionAttempt(lastResult, attempt);
            }

            logger.LogWarning(
                "Collector will retry collection. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, RetryCount={RetryCount}",
                device.Id,
                device.ProtocolType,
                lastResult.FaultType,
                attempt + 1);
            await Task.Delay(collectorOptions.RetryDelayMs, cancellationToken);
        }

        return new CollectionAttempt(
            lastResult ?? CollectionResult.Failed(FaultType.Unknown, "NoCollectionAttempt", "No collection attempt was made.", BuildRawRequest()),
            collectorOptions.MaxRetry);
    }

    private static string BuildRawRequest() => "Protocol adapter unavailable";

    private (IProtocolAdapter Adapter, Func<CancellationToken, Task<ConnectionResult>> ConnectAsync, Func<CancellationToken, Task<CollectionResult>> CollectAsync)
        BuildProtocolOperations(Device device, DeviceProtocolSettings settings) => device.ProtocolType switch
        {
            ProtocolType.ModbusTcp => (
                modbusAdapter,
                token => modbusAdapter.ConnectAsync(device, token),
                token => modbusAdapter.CollectAsync(device, settings.ToModbusConfiguration(), token)),
            ProtocolType.OpcUa => (
                opcUaAdapter,
                token => opcUaAdapter.ConnectAsync(device, settings.ToOpcUaConfiguration(GetOpcUaEndpoint(device)), token),
                token => opcUaAdapter.CollectAsync(device, settings.ToOpcUaConfiguration(GetOpcUaEndpoint(device)), token)),
            ProtocolType.Mqtt => (
                mqttAdapter,
                token => mqttAdapter.ConnectAsync(device, settings.ToMqttConfiguration(), token),
                token => mqttAdapter.CollectAsync(device, settings.ToMqttConfiguration(), token)),
            _ => throw new InvalidOperationException($"No adapter is registered for protocol '{device.ProtocolType}'.")
        };

    private static string GetOpcUaEndpoint(Device device) =>
        device.Host.StartsWith("opc.tcp://", StringComparison.OrdinalIgnoreCase)
            ? device.Host
            : $"opc.tcp://{device.Host}:{device.Port}";

    private sealed record CollectionAttempt(CollectionResult Result, int RetryCount);
}
