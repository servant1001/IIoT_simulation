using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Collection;

public sealed record CollectorDeviceResult(
    Guid DeviceId,
    Guid CommunicationRecordId,
    bool Success,
    FaultType FaultType,
    int RetryCount,
    double ResponseTimeMs);
