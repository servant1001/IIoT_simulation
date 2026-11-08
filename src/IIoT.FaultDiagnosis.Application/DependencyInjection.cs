using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Application.Metrics;
using IIoT.FaultDiagnosis.Application.Experiments;
using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Application.Datasets;
using IIoT.FaultDiagnosis.Application.Rag;
using IIoT.FaultDiagnosis.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.FaultDiagnosis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ProcessMetricsSampler>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDeviceConnectionService, DeviceConnectionService>();
        services.AddScoped<ICollectorService, CollectorService>();
        services.AddScoped<IMetricService, MetricService>();
        services.AddSingleton<IExperimentService, ExperimentService>();
        AddFaultRules(services);
        services.AddSingleton<IFaultDiagnosisEngine, RuleBasedFaultDiagnosisEngine>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddScoped<IDatasetService, DatasetService>();
        services.AddSingleton<IRagKnowledgeBase, RagKnowledgeBase>();
        services.AddScoped<IRagDiagnosisService, RagDiagnosisService>();
        return services;
    }

    private static void AddFaultRules(IServiceCollection services)
    {
        var rules = new[]
        {
            new FaultTypeRule(FaultType.None, "The communication completed successfully and no protocol fault was observed.", "Continue normal monitoring and retain the successful communication record as a baseline."),
            new FaultTypeRule(FaultType.ConnectionRefused, "The target endpoint refused the TCP connection.", "Confirm host, port, firewall and simulator or PLC availability."),
            new FaultTypeRule(FaultType.Timeout, "The device did not respond before the configured timeout.", "Check device load, network latency and the operation timeout after confirming the request is valid."),
            new FaultTypeRule(FaultType.IllegalAddress, "The requested Modbus register address is not supported.", "Confirm the slave ID, register type and Modbus register map."),
            new FaultTypeRule(FaultType.InvalidEndpoint, "The OPC UA endpoint URL or port is not published by the server.", "Copy the endpoint from the OPC UA server configuration and verify the server is running."),
            new FaultTypeRule(FaultType.InvalidNodeId, "The requested OPC UA NodeId is not exposed by the server namespace.", "Browse the OPC UA address space and verify the NodeId and namespace index."),
            new FaultTypeRule(FaultType.CertificateError, "OPC UA security validation rejected a client or server certificate.", "Trust the required certificate and verify the selected security endpoint configuration."),
            new FaultTypeRule(FaultType.SessionDisconnected, "The OPC UA session or secure channel was closed during communication.", "Check server availability, secure-channel lifetime and reconnect behaviour before retrying."),
            new FaultTypeRule(FaultType.BrokerUnavailable, "The MQTT broker is unavailable at the configured endpoint.", "Confirm broker availability, host, port and Docker port mapping."),
            new FaultTypeRule(FaultType.AuthenticationFailed, "The MQTT broker rejected the configured credentials.", "Verify username, password and broker ACL permissions."),
            new FaultTypeRule(FaultType.TopicMismatch, "The MQTT subscription topic was rejected or did not match the publisher topic.", "Compare topic names exactly and verify topic ACL permissions."),
            new FaultTypeRule(FaultType.PayloadInvalid, "The MQTT payload does not satisfy the configured JSON or schema expectation.", "Capture the raw payload and align publisher encoding and required JSON fields.")
        };

        foreach (var rule in rules)
        {
            services.AddSingleton<IFaultRule>(rule);
        }
    }
}
