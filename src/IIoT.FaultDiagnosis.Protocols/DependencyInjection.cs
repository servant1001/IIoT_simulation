using IIoT.FaultDiagnosis.Protocols.Abstractions;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using IIoT.FaultDiagnosis.Protocols.Mqtt;
using IIoT.FaultDiagnosis.Protocols.OpcUa;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.FaultDiagnosis.Protocols;

public static class DependencyInjection
{
    public static IServiceCollection AddProtocols(this IServiceCollection services)
    {
        services.AddScoped<IModbusProtocolAdapter, ModbusProtocolAdapter>();
        services.AddScoped<IOpcUaProtocolAdapter, OpcUaProtocolAdapter>();
        services.AddScoped<IMqttProtocolAdapter, MqttProtocolAdapter>();
        services.AddScoped<IProtocolAdapter>(serviceProvider => serviceProvider.GetRequiredService<IModbusProtocolAdapter>());
        services.AddScoped<IProtocolAdapter>(serviceProvider => serviceProvider.GetRequiredService<IOpcUaProtocolAdapter>());
        services.AddScoped<IProtocolAdapter>(serviceProvider => serviceProvider.GetRequiredService<IMqttProtocolAdapter>());
        return services;
    }
}
