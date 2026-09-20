using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class DeviceRepository(FaultDiagnosisDbContext dbContext) : IDeviceRepository
{
    public Task AddAsync(Device device, CancellationToken cancellationToken) => dbContext.Devices.AddAsync(device, cancellationToken).AsTask();

    public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Devices.SingleOrDefaultAsync(device => device.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Device>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Devices.OrderBy(device => device.Name).ToListAsync(cancellationToken);

    public Task RemoveAsync(Device device, CancellationToken cancellationToken)
    {
        dbContext.Devices.Remove(device);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
