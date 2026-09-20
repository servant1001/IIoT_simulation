using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Abstractions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace IIoT.FaultDiagnosis.Protocols.Mqtt;

public interface IMqttProtocolAdapter : IProtocolAdapter
{
    Task<ConnectionResult> ConnectAsync(Device device, MqttConfiguration configuration, CancellationToken cancellationToken);
    Task<CollectionResult> CollectAsync(Device device, MqttConfiguration configuration, CancellationToken cancellationToken);
}

public sealed class MqttProtocolAdapter(ILogger<MqttProtocolAdapter> logger) : IMqttProtocolAdapter
{
    private readonly ConcurrentDictionary<Guid, MqttSession> sessions = new();

    public ProtocolType ProtocolType => ProtocolType.Mqtt;

    public Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken) =>
        ConnectAsync(device, MqttConfiguration.Default, cancellationToken);

    public async Task<ConnectionResult> ConnectAsync(
        Device device,
        MqttConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        if (sessions.TryGetValue(device.Id, out var existing) && existing.Client.IsConnected)
            return ConnectionResult.Connected();

        if (existing is not null)
            await DisconnectAsync(device, CancellationToken.None);

        var client = new MqttClientFactory().CreateMqttClient();
        var messages = Channel.CreateUnbounded<MqttApplicationMessage>();
        var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += eventArgs =>
        {
            messages.Writer.TryWrite(eventArgs.ApplicationMessage);
            return Task.CompletedTask;
        };
        client.DisconnectedAsync += _ =>
        {
            disconnected.TrySetResult(true);
            messages.Writer.TryComplete();
            return Task.CompletedTask;
        };

        try
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId($"iiot-fault-diagnosis-{device.Id:N}")
                .WithTcpServer(device.Host, device.Port)
                .WithCleanSession();
            if (!string.IsNullOrWhiteSpace(configuration.Username))
                optionsBuilder.WithCredentials(configuration.Username, configuration.Password);

            var connectResult = await client.ConnectAsync(optionsBuilder.Build(), cancellationToken);
            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                client.Dispose();
                var errorCode = connectResult.ResultCode.ToString();
                var errorMessage = string.IsNullOrWhiteSpace(connectResult.ReasonString)
                    ? $"MQTT broker rejected the connection: {errorCode}."
                    : connectResult.ReasonString;
                var faultType = MqttFaultNormalizer.Normalize(new InvalidOperationException(errorCode));
                logger.LogWarning(
                    "MQTT broker rejected connection. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ErrorCode={ErrorCode}",
                    device.Id,
                    ProtocolType,
                    faultType,
                    errorCode);
                return ConnectionResult.Failed(faultType, errorCode, errorMessage);
            }

            sessions[device.Id] = new MqttSession(client, configuration, messages, disconnected);
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
            var faultType = MqttFaultNormalizer.Normalize(exception);
            logger.LogWarning(
                exception,
                "MQTT connection failed. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ErrorCode={ErrorCode}",
                device.Id,
                ProtocolType,
                faultType,
                exception.GetType().Name);
            return ConnectionResult.Failed(faultType, exception.GetType().Name, exception.Message);
        }
    }

    public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken) =>
        CollectAsync(device, MqttConfiguration.Default, cancellationToken);

    public async Task<CollectionResult> CollectAsync(
        Device device,
        MqttConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        var rawRequest = $"Topic={configuration.Topic};QoS={configuration.QualityOfService}";
        if (!sessions.TryGetValue(device.Id, out var session) || !session.Client.IsConnected)
            return CollectionResult.Failed(FaultType.ConnectionLost, "NotConnected", "No active MQTT connection exists for this device.", rawRequest);

        return await CollectFromSessionAsync(device, configuration, session, rawRequest, allowReconnect: true, cancellationToken);
    }

    public async Task DisconnectAsync(Device device, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!sessions.TryRemove(device.Id, out var session))
            return;

        try
        {
            if (session.Client.IsConnected)
                await session.Client.DisconnectAsync(cancellationToken: cancellationToken);
        }
        finally
        {
            session.Client.Dispose();
        }
    }

    private async Task<CollectionResult> CollectFromSessionAsync(
        Device device,
        MqttConfiguration configuration,
        MqttSession session,
        string rawRequest,
        bool allowReconnect,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(filter => filter
                    .WithTopic(configuration.Topic)
                    .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)configuration.QualityOfService))
                .Build();
            await session.Client.SubscribeAsync(subscribeOptions, cancellationToken);

            var readTask = session.Messages.Reader.ReadAsync(cancellationToken).AsTask();
            var timeoutTask = Task.Delay(configuration.ReceiveTimeoutMs, cancellationToken);
            var completed = await Task.WhenAny(readTask, timeoutTask, session.Disconnected.Task);
            if (completed == readTask)
            {
                try
                {
                    var message = await readTask;
                    return ToCollectionResult(message, configuration, rawRequest);
                }
                catch (ChannelClosedException) when (!session.Client.IsConnected)
                {
                    return await ReconnectAndCollectAsync(
                        device,
                        configuration,
                        rawRequest,
                        allowReconnect,
                        cancellationToken);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (completed == timeoutTask)
                return CollectionResult.Failed(FaultType.Timeout, "ReceiveTimeout", "No MQTT message arrived before the receive timeout.", rawRequest);

            return await ReconnectAndCollectAsync(device, configuration, rawRequest, allowReconnect, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var faultType = MqttFaultNormalizer.Normalize(exception);
            logger.LogWarning(
                exception,
                "MQTT collection failed. DeviceId={DeviceId}, Protocol={Protocol}, FaultType={FaultType}, ErrorCode={ErrorCode}",
                device.Id,
                ProtocolType,
                faultType,
                exception.GetType().Name);
            return CollectionResult.Failed(faultType, exception.GetType().Name, exception.Message, rawRequest);
        }
    }

    private static CollectionResult ToCollectionResult(
        MqttApplicationMessage message,
        MqttConfiguration configuration,
        string rawRequest)
    {
        if (!string.Equals(message.Topic, configuration.Topic, StringComparison.Ordinal))
        {
            return CollectionResult.Failed(
                FaultType.TopicMismatch,
                "TopicMismatch",
                $"Received topic '{message.Topic}' does not match configured topic '{configuration.Topic}'.",
                rawRequest);
        }

        var payload = message.ConvertPayloadToString();
        if (!configuration.RequireJsonPayload)
            return new CollectionResult(true, FaultType.None, null, null, rawRequest, payload, payload);

        try
        {
            using var document = JsonDocument.Parse(payload);
            return new CollectionResult(true, FaultType.None, null, null, rawRequest, payload, document.RootElement.GetRawText());
        }
        catch (JsonException exception)
        {
            return CollectionResult.Failed(FaultType.PayloadInvalid, exception.GetType().Name, exception.Message, rawRequest);
        }
    }

    private async Task<CollectionResult> ReconnectAndCollectAsync(
        Device device,
        MqttConfiguration configuration,
        string rawRequest,
        bool allowReconnect,
        CancellationToken cancellationToken)
    {
        if (!allowReconnect)
            return CollectionResult.Failed(FaultType.ConnectionLost, "Disconnected", "The MQTT client disconnected while waiting for a message.", rawRequest);

        await Task.Delay(configuration.ReconnectDelayMs, cancellationToken);
        var reconnectResult = await ConnectAsync(device, configuration, cancellationToken);
        if (!reconnectResult.Success || !sessions.TryGetValue(device.Id, out var reconnectedSession))
        {
            return CollectionResult.Failed(
                reconnectResult.FaultType,
                reconnectResult.ErrorCode,
                reconnectResult.ErrorMessage,
                rawRequest);
        }

        return await CollectFromSessionAsync(device, configuration, reconnectedSession, rawRequest, allowReconnect: false, cancellationToken);
    }

    private sealed record MqttSession(
        IMqttClient Client,
        MqttConfiguration Configuration,
        Channel<MqttApplicationMessage> Messages,
        TaskCompletionSource<bool> Disconnected);
}
