using IIoT.FaultDiagnosis.Domain.Entities;
namespace IIoT.FaultDiagnosis.Application.Diagnosis;
public sealed record DiagnosisEvaluation(int TotalCount, int CorrectCount, decimal Accuracy);
public interface IDiagnosisRepository { Task<CommunicationRecord?> GetRecordAsync(Guid id,CancellationToken ct); Task AddAsync(DiagnosisResult result,CancellationToken ct); Task<DiagnosisEvaluation?> GetEvaluationAsync(Guid experimentId,CancellationToken ct); Task SaveChangesAsync(CancellationToken ct); }
public interface IDiagnosisService { Task<DiagnosisResult?> DiagnoseAsync(Guid recordId,CancellationToken ct); Task<DiagnosisEvaluation?> GetEvaluationAsync(Guid experimentId,CancellationToken ct); }
public sealed class DiagnosisService(IDiagnosisRepository repository, IFaultDiagnosisEngine engine) : IDiagnosisService
{ public async Task<DiagnosisResult?> DiagnoseAsync(Guid recordId,CancellationToken ct) { var record=await repository.GetRecordAsync(recordId,ct); if(record is null)return null; var result=await engine.DiagnoseAsync(record,ct); await repository.AddAsync(result,ct); await repository.SaveChangesAsync(ct); return result; } public Task<DiagnosisEvaluation?> GetEvaluationAsync(Guid experimentId,CancellationToken ct)=>repository.GetEvaluationAsync(experimentId,ct); }
