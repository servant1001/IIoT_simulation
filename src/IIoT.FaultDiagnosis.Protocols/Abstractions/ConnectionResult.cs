using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Protocols.Abstractions;

public sealed record ConnectionResult(bool Success, FaultType FaultType, string? ErrorCode, string? ErrorMessage)
{
    public static ConnectionResult Connected() => new(true, FaultType.None, null, null);

    public static ConnectionResult Failed(FaultType faultType, string? errorCode, string? errorMessage) =>
        new(false, faultType, errorCode, errorMessage);
}
