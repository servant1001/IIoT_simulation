using IIoT.FaultDiagnosis.Protocols.Modbus;
using IIoT.FaultDiagnosis.Protocols.OpcUa;

namespace IIoT.FaultDiagnosis.Application.Communication;

public interface IDeviceConnectionService
{
    Task<DeviceConnectionTestResult?> TestModbusAsync(
        Guid deviceId,
        ModbusConfiguration configuration,
        CancellationToken cancellationToken);

    Task<DeviceConnectionTestResult?> TestOpcUaAsync(
        Guid deviceId,
        OpcUaConfiguration configuration,
        CancellationToken cancellationToken);
}
