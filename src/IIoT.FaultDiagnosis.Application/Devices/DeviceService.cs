using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Application.Devices;

public sealed class DeviceService(IDeviceRepository repository, TimeProvider timeProvider) : IDeviceService
{
    public async Task<DeviceDto> CreateAsync(CreateDeviceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var device = new Device(
            Guid.NewGuid(),
            command.Name,
            command.ProtocolType,
            command.Host,
            command.Port,
            command.IsEnabled,
            timeProvider.GetUtcNow());

        await repository.AddAsync(device, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return DeviceDto.FromEntity(device);
    }

    public async Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(id, cancellationToken);
        return device is null ? null : DeviceDto.FromEntity(device);
    }

    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken cancellationToken)
    {
        var devices = await repository.ListAsync(cancellationToken);
        return devices.Select(DeviceDto.FromEntity).ToArray();
    }

    public async Task<DeviceDto?> UpdateAsync(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var device = await repository.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            return null;
        }

        device.Update(
            command.Name,
            command.ProtocolType,
            command.Host,
            command.Port,
            command.IsEnabled,
            timeProvider.GetUtcNow());

        await repository.SaveChangesAsync(cancellationToken);
        return DeviceDto.FromEntity(device);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            return false;
        }

        await repository.RemoveAsync(device, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
