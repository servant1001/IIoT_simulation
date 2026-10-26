using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Application.Rag;

public interface IRagDiagnosisRepository
{
    Task<RagRecordContext?> GetContextAsync(Guid communicationRecordId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RagHistoricalCase>> GetHistoricalCasesAsync(RagRecordContext context, int maximumCount, CancellationToken cancellationToken);
    Task AddTraceAsync(RagDiagnosisTrace trace, CancellationToken cancellationToken);
    Task<RagEvaluation?> GetEvaluationAsync(Guid experimentId, CancellationToken cancellationToken);
}
