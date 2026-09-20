using System.ComponentModel.DataAnnotations;

namespace IIoT.FaultDiagnosis.Api.Contracts.Devices;

public sealed class TestOpcUaConnectionRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> NodeIds { get; init; } = ["i=2258"];

    public bool UseSecurity { get; init; }

    [Range(100, 120000)]
    public int OperationTimeoutMs { get; init; } = 10000;
}
