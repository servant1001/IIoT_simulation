using System.ComponentModel.DataAnnotations;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Api.Contracts.Experiments;

public sealed class CreateExperimentRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [EnumDataType(typeof(ProtocolType))] public ProtocolType ProtocolType { get; init; } = ProtocolType.ModbusTcp;
    [EnumDataType(typeof(FaultType))] public FaultType ActualFaultType { get; init; } = FaultType.None;
    [Range(1, 1000)] public int DeviceCount { get; init; } = 1;
    [Range(1, 10000)] public int TagCount { get; init; } = 6;
    [Range(100, 60000)] public int PollingIntervalMs { get; init; } = 1000;
    [Range(1, 86400)] public int DurationSeconds { get; init; } = 300;
    [Range(1, 1000)] public int RepeatCount { get; init; } = 1;
}
