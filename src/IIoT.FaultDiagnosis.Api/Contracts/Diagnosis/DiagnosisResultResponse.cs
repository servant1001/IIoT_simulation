using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Api.Contracts.Diagnosis;

public sealed record DiagnosisResultResponse(
    Guid Id,
    Guid CommunicationRecordId,
    DiagnosisMethod DiagnosisMethod,
    FaultType PredictedFaultType,
    decimal? Confidence,
    string? PossibleCauses,
    string? SuggestedActions,
    double DiagnosisDurationMs,
    DateTimeOffset CreatedAt)
{
    public static DiagnosisResultResponse From(DiagnosisResult result) => new(
        result.Id,
        result.CommunicationRecordId,
        result.DiagnosisMethod,
        result.PredictedFaultType,
        result.Confidence,
        result.PossibleCauses,
        result.SuggestedActions,
        result.DiagnosisDurationMs,
        result.CreatedAt);
}
