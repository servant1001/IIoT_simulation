using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Rag;

public sealed record RagRecordContext(
    Guid CommunicationRecordId,
    ProtocolType ProtocolType,
    string DeviceHost,
    int DevicePort,
    CommunicationStatus Status,
    FaultType ObservedFaultType,
    string? ErrorCode,
    string? ErrorMessage,
    double? ResponseTimeMs,
    int RetryCount);

public sealed record RagHistoricalCase(
    Guid CommunicationRecordId,
    ProtocolType ProtocolType,
    CommunicationStatus Status,
    FaultType ObservedFaultType,
    string? ErrorCode,
    string? ErrorMessage,
    double? ResponseTimeMs,
    int RetryCount,
    DateTimeOffset CreatedAt);

public sealed record RagKnowledgeArticle(
    string Id,
    string Title,
    ProtocolType? ProtocolType,
    FaultType FaultType,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> PossibleCauses,
    IReadOnlyList<string> SuggestedActions);

public sealed record RagSource(string Id, string Title, FaultType FaultType, int Score);

public sealed record RagDiagnosisResponse(
    Guid CommunicationRecordId,
    FaultType PredictedFaultType,
    IReadOnlyList<FaultType> CandidateFaultTypes,
    IReadOnlyList<string> PossibleCauses,
    IReadOnlyList<string> SuggestedActions,
    decimal Confidence,
    double DiagnosisDurationMs,
    IReadOnlyList<RagSource> Sources,
    IReadOnlyList<RagHistoricalCase> HistoricalCases);

public sealed record RagEvaluation(
    int TotalCount,
    int Top1CorrectCount,
    int Top3CorrectCount,
    decimal Top1Accuracy,
    decimal Top3Accuracy,
    double AverageDiagnosisDurationMs);
