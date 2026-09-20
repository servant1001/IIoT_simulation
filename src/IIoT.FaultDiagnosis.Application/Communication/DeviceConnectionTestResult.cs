using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Communication;

public sealed record DeviceConnectionTestResult(
    Guid CommunicationRecordId,
    bool Success,
    double ResponseTimeMs,
    FaultType FaultType,
    string? ErrorCode,
    string? Message,
    string? ParsedValue);
