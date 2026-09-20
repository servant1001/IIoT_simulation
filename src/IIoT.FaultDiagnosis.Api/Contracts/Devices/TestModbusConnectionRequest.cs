using System.ComponentModel.DataAnnotations;

namespace IIoT.FaultDiagnosis.Api.Contracts.Devices;

public sealed class TestModbusConnectionRequest
{
    [Range(1, 247)]
    public int SlaveId { get; init; } = 1;

    [Range(0, ushort.MaxValue)]
    public int StartAddress { get; init; }

    [Range(1, 125)]
    public int NumberOfPoints { get; init; } = 6;
}
