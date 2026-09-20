using System.ComponentModel.DataAnnotations;

namespace IIoT.FaultDiagnosis.Api.Contracts.Devices;

public sealed class TestMqttConnectionRequest
{
    [Required]
    [MinLength(1)]
    public string Topic { get; init; } = "factory/machine01/data";

    [Range(0, 2)]
    public int QualityOfService { get; init; } = 1;

    [Range(100, 120000)]
    public int ReceiveTimeoutMs { get; init; } = 10000;

    [Range(100, 120000)]
    public int ReconnectDelayMs { get; init; } = 1000;

    public bool RequireJsonPayload { get; init; } = true;

    public string? Username { get; init; }

    public string? Password { get; init; }
}
