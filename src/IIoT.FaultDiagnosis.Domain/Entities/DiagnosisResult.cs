using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class DiagnosisResult
{
    private DiagnosisResult() { }
    public DiagnosisResult(Guid id, Guid communicationRecordId, DiagnosisMethod diagnosisMethod, FaultType predictedFaultType, decimal confidence, string possibleCauses, string suggestedActions, double diagnosisDurationMs, DateTimeOffset createdAt)
    {
        Id = id; CommunicationRecordId = communicationRecordId; DiagnosisMethod = diagnosisMethod; PredictedFaultType = predictedFaultType;
        Confidence = confidence; PossibleCauses = possibleCauses; SuggestedActions = suggestedActions; DiagnosisDurationMs = diagnosisDurationMs; CreatedAt = createdAt;
    }
    public Guid Id { get; private set; }
    public Guid CommunicationRecordId { get; private set; }
    public DiagnosisMethod DiagnosisMethod { get; private set; }
    public FaultType PredictedFaultType { get; private set; }
    public decimal? Confidence { get; private set; }
    public string? PossibleCauses { get; private set; }
    public string? SuggestedActions { get; private set; }
    public double DiagnosisDurationMs { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public CommunicationRecord CommunicationRecord { get; private set; } = null!;
}
