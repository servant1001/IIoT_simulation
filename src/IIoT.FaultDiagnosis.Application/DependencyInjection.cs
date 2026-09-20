using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Application.Metrics;
using IIoT.FaultDiagnosis.Application.Experiments;
using IIoT.FaultDiagnosis.Application.Diagnosis;
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
        services.AddSingleton<IFaultRule>(new FaultTypeRule(FaultType.ConnectionRefused, "The target endpoint refused the TCP connection.", "Confirm host, port and simulator availability."));
        services.AddSingleton<IFaultRule>(new FaultTypeRule(FaultType.Timeout, "The device did not respond before the timeout.", "Check device load and network latency."));
        services.AddSingleton<IFaultRule>(new FaultTypeRule(FaultType.IllegalAddress, "The requested register address is not supported.", "Confirm the Modbus register map."));
        services.AddSingleton<IFaultDiagnosisEngine, RuleBasedFaultDiagnosisEngine>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        return services;
    }
}
