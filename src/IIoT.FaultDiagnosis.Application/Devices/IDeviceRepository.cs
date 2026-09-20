using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Application.Devices;

public interface IDeviceRepository
{
    Task AddAsync(Device device, CancellationToken cancellationToken);
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Device>> ListAsync(CancellationToken cancellationToken);
    Task RemoveAsync(Device device, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
