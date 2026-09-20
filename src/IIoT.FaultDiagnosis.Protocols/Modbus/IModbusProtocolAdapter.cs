using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Protocols.Abstractions;

namespace IIoT.FaultDiagnosis.Protocols.Modbus;

public interface IModbusProtocolAdapter : IProtocolAdapter
{
    Task<CollectionResult> CollectAsync(
        Device device,
        ModbusConfiguration configuration,
        CancellationToken cancellationToken);
}
