using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Devices;

public sealed record DeviceDto(
    Guid Id,
    string Name,
    ProtocolType ProtocolType,
    string Host,
    int Port,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static DeviceDto FromEntity(Device device) => new(
        device.Id,
        device.Name,
        device.ProtocolType,
        device.Host,
        device.Port,
        device.IsEnabled,
        device.CreatedAt,
        device.UpdatedAt);
}
