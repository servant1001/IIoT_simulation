using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Protocols.Abstractions;

public sealed record CollectionResult(
    bool Success,
    FaultType FaultType,
    string? ErrorCode,
    string? ErrorMessage,
    string RawRequest,
    string? RawResponse,
    string? ParsedValue)
{
    public static CollectionResult Failed(
        FaultType faultType,
        string? errorCode,
        string? errorMessage,
        string rawRequest) =>
        new(false, faultType, errorCode, errorMessage, rawRequest, null, null);
}
