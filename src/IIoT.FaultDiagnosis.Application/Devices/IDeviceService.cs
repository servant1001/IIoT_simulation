namespace IIoT.FaultDiagnosis.Application.Devices;

public interface IDeviceService
{
    Task<DeviceDto> CreateAsync(CreateDeviceCommand command, CancellationToken cancellationToken);
    Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken cancellationToken);
    Task<DeviceDto?> UpdateAsync(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
