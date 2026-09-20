using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Datasets;

public sealed record DatasetRecord(
    Guid CommunicationRecordId,
    Guid ExperimentId,
    Guid? ExperimentRunId,
    int? RunNumber,
    Guid DeviceId,
    ProtocolType ProtocolType,
    FaultType ActualFaultType,
    FaultType? PredictedFaultType,
    DiagnosisMethod? DiagnosisMethod,
    decimal? DiagnosisConfidence,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    double? ResponseTimeMs,
    CommunicationStatus Status,
    FaultType ObservedFaultType,
    string? ErrorCode,
    string? ErrorMessage,
    int RetryCount,
    string? RawRequest,
    string? RawResponse,
    string? ParsedValue);

public sealed record DatasetQuery(ProtocolType? ProtocolType = null, FaultType? ActualFaultType = null, bool OnlyDiagnosed = false);

public sealed record DatasetExport(string FileName, byte[] Content);

public sealed record DatasetCoverage(
    ProtocolType ProtocolType,
    FaultType ActualFaultType,
    int RunCount,
    int RecordCount,
    int DiagnosedRecordCount,
    int RequiredRunCount,
    int RemainingRunCount);

public sealed record DatasetSummary(int RecordCount, int ExperimentCount, IReadOnlyList<DatasetCoverage> Coverage);
