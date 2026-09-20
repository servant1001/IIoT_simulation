using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Protocols.Abstractions;

public interface IProtocolAdapter
{
    ProtocolType ProtocolType { get; }

    Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken);

    Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken);

    Task DisconnectAsync(Device device, CancellationToken cancellationToken);
}
