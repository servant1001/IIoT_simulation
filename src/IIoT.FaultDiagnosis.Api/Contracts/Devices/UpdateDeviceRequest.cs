using System.ComponentModel.DataAnnotations;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Api.Contracts.Devices;

public sealed class UpdateDeviceRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [EnumDataType(typeof(ProtocolType))]
    public ProtocolType ProtocolType { get; init; }

    [Required]
    [StringLength(255)]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool IsEnabled { get; init; } = true;
}
