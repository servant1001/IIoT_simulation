namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class RagDiagnosisTrace
{
    private RagDiagnosisTrace()
    {
    }

    public RagDiagnosisTrace(Guid id, Guid diagnosisResultId, string candidateFaultTypes, string sourceIds, string historicalCaseIds, string provider, DateTimeOffset createdAt)
    {
        Id = id;
        DiagnosisResultId = diagnosisResultId;
        CandidateFaultTypes = candidateFaultTypes;
        SourceIds = sourceIds;
        HistoricalCaseIds = historicalCaseIds;
        Provider = provider;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid DiagnosisResultId { get; private set; }
    public string CandidateFaultTypes { get; private set; } = "[]";
    public string SourceIds { get; private set; } = "[]";
    public string HistoricalCaseIds { get; private set; } = "[]";
    public string Provider { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public DiagnosisResult DiagnosisResult { get; private set; } = null!;
}
