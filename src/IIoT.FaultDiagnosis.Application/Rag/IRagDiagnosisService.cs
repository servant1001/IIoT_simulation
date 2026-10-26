namespace IIoT.FaultDiagnosis.Application.Rag;

public interface IRagDiagnosisService
{
    Task<RagDiagnosisResponse?> DiagnoseAsync(Guid communicationRecordId, CancellationToken cancellationToken);
    Task<RagEvaluation?> GetEvaluationAsync(Guid experimentId, CancellationToken cancellationToken);
}
