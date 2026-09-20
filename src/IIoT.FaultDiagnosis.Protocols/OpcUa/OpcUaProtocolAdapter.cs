using System.Collections.Concurrent;
using System.Text.Json;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Abstractions;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace IIoT.FaultDiagnosis.Protocols.OpcUa;

public interface IOpcUaProtocolAdapter : IProtocolAdapter
{
    Task<ConnectionResult> ConnectAsync(Device device, OpcUaConfiguration configuration, CancellationToken cancellationToken);
    Task<CollectionResult> CollectAsync(Device device, OpcUaConfiguration configuration, CancellationToken cancellationToken);
}

public sealed class OpcUaProtocolAdapter : IOpcUaProtocolAdapter
{
    private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
    private readonly ConcurrentDictionary<Guid, ISession> sessions = new();
    public ProtocolType ProtocolType => ProtocolType.OpcUa;

    public Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken) =>
        ConnectAsync(device, new OpcUaConfiguration(GetEndpoint(device), ["i=2258"]), cancellationToken);

    public async Task<ConnectionResult> ConnectAsync(
        Device device,
        OpcUaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            configuration.Validate();
            if (sessions.TryGetValue(device.Id, out var existing) && existing.Connected)
            {
                return ConnectionResult.Connected();
            }

            if (existing is not null)
            {
                await CloseAndDisposeAsync(existing, CancellationToken.None);
            }

            var applicationConfiguration = CreateConfiguration(configuration.OperationTimeoutMs);
            await applicationConfiguration.ValidateAsync(ApplicationType.Client);
            var application = new ApplicationInstance(applicationConfiguration, Telemetry);
            if (!await application.CheckApplicationInstanceCertificatesAsync(false, 2048, cancellationToken))
            {
                return ConnectionResult.Failed(
                    FaultType.CertificateError,
                    "ApplicationCertificate",
                    "The OPC UA client application certificate could not be loaded or created.");
            }

            var endpoint = await CoreClientUtils.SelectEndpointAsync(
                applicationConfiguration,
                GetEndpoint(device),
                configuration.UseSecurity,
                configuration.OperationTimeoutMs,
                Telemetry,
                cancellationToken);
            var session = await new DefaultSessionFactory(Telemetry).CreateAsync(
                applicationConfiguration,
                new ConfiguredEndpoint(null, endpoint, null),
                updateBeforeConnect: false,
                checkDomain: false,
                sessionName: "IIoT Fault Diagnosis",
                sessionTimeout: 60000,
                identity: null,
                preferredLocales: null,
                cancellationToken);
            sessions[device.Id] = session;
            return ConnectionResult.Connected();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ConnectionResult.Failed(
                OpcUaFaultNormalizer.Normalize(exception),
                exception.GetType().Name,
                exception.Message);
        }
    }

    public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken) =>
        CollectAsync(device, new OpcUaConfiguration(GetEndpoint(device), ["i=2258"]), cancellationToken);

    public async Task<CollectionResult> CollectAsync(Device device, OpcUaConfiguration configuration, CancellationToken cancellationToken)
    {
        configuration.Validate();
        var raw = string.Join(";", configuration.NodeIds.Select(nodeId => $"NodeId={nodeId}"));
        if (!sessions.TryGetValue(device.Id, out var session) || !session.Connected)
        {
            return CollectionResult.Failed(
                FaultType.SessionDisconnected,
                "NotConnected",
                "No active OPC UA session exists.",
                raw);
        }

        try
        {
            return await ReadNodesAsync(session, configuration.NodeIds, raw, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var faultType = OpcUaFaultNormalizer.Normalize(exception);
            if (faultType != FaultType.SessionDisconnected)
            {
                return CollectionResult.Failed(faultType, exception.GetType().Name, exception.Message, raw);
            }

            await DisconnectAsync(device, CancellationToken.None);
            await Task.Delay(configuration.ReconnectDelayMs, cancellationToken);
            var reconnectResult = await ConnectAsync(device, configuration, cancellationToken);
            if (!reconnectResult.Success || !sessions.TryGetValue(device.Id, out var reconnectedSession))
            {
                return CollectionResult.Failed(
                    reconnectResult.FaultType,
                    reconnectResult.ErrorCode,
                    reconnectResult.ErrorMessage,
                    raw);
            }

            try
            {
                return await ReadNodesAsync(reconnectedSession, configuration.NodeIds, raw, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception reconnectException)
            {
                return CollectionResult.Failed(
                    OpcUaFaultNormalizer.Normalize(reconnectException),
                    reconnectException.GetType().Name,
                    reconnectException.Message,
                    raw);
            }
        }
    }

    public async Task DisconnectAsync(Device device, CancellationToken cancellationToken)
    {
        if (sessions.TryRemove(device.Id, out var session))
        {
            await CloseAndDisposeAsync(session, cancellationToken);
        }
    }

    private static async Task CloseAndDisposeAsync(ISession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.CloseAsync(closeChannel: true, cancellationToken);
        }
        finally
        {
            session.Dispose();
        }
    }

    private static ApplicationConfiguration CreateConfiguration(int operationTimeoutMs) => new()
    {
        ApplicationName = "IIoT Fault Diagnosis",
        ApplicationUri = "urn:iiot-fault-diagnosis:opcua-client",
        ApplicationType = ApplicationType.Client,
        ClientConfiguration = new ClientConfiguration(),
        SecurityConfiguration = new SecurityConfiguration
        {
            ApplicationCertificate = new CertificateIdentifier
            {
                StoreType = CertificateStoreType.Directory,
                StorePath = Path.Combine(GetPkiRoot(), "own-v2"),
                SubjectName = "CN=IIoTFaultDiagnosis, O=IIoTFaultDiagnosis"
            },
            TrustedPeerCertificates = new CertificateTrustList
            {
                StoreType = CertificateStoreType.Directory,
                StorePath = Path.Combine(GetPkiRoot(), "trusted")
            },
            TrustedIssuerCertificates = new CertificateTrustList
            {
                StoreType = CertificateStoreType.Directory,
                StorePath = Path.Combine(GetPkiRoot(), "issuers")
            },
            RejectedCertificateStore = new CertificateTrustList
            {
                StoreType = CertificateStoreType.Directory,
                StorePath = Path.Combine(GetPkiRoot(), "rejected")
            },
            AutoAcceptUntrustedCertificates = false
        },
        TransportQuotas = new TransportQuotas
        {
            OperationTimeout = operationTimeoutMs
        }
    };

    private static async Task<CollectionResult> ReadNodesAsync(
        ISession session,
        IReadOnlyList<string> nodeIds,
        string rawRequest,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>();
        foreach (var nodeId in nodeIds)
        {
            var value = await session.ReadValueAsync(NodeId.Parse(nodeId), cancellationToken);
            values[nodeId] = value.Value;
        }

        var payload = JsonSerializer.Serialize(values);
        return new CollectionResult(true, FaultType.None, null, null, rawRequest, payload, payload);
    }

    private static string GetEndpoint(Device device)
    {
        if (Uri.TryCreate(device.Host, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme.Equals("opc.tcp", StringComparison.OrdinalIgnoreCase))
        {
            return device.Host;
        }

        return $"opc.tcp://{device.Host}:{device.Port}";
    }

    private static string GetPkiRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IIoTFaultDiagnosis",
        "pki");
}
