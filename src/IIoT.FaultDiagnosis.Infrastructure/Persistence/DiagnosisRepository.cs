using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Application.Rag;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;
public sealed class DiagnosisRepository(FaultDiagnosisDbContext db) : IDiagnosisRepository, IRagDiagnosisRepository
{
    public Task<CommunicationRecord?> GetRecordAsync(Guid id, CancellationToken ct) => db.CommunicationRecords.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(DiagnosisResult result, CancellationToken ct) => db.DiagnosisResults.AddAsync(result, ct).AsTask();

    public async Task<RagRecordContext?> GetContextAsync(Guid communicationRecordId, CancellationToken ct) =>
        await (from record in db.CommunicationRecords.AsNoTracking()
               join device in db.Devices.AsNoTracking() on record.DeviceId equals device.Id
               where record.Id == communicationRecordId
               select new RagRecordContext(
                   record.Id, record.ProtocolType, device.Host, device.Port, record.Status, record.FaultType,
                   record.ErrorCode, record.ErrorMessage, record.ResponseTimeMs, record.RetryCount))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<RagHistoricalCase>> GetHistoricalCasesAsync(RagRecordContext context, int maximumCount, CancellationToken ct) =>
        await db.CommunicationRecords.AsNoTracking()
            .Where(record => record.Id != context.CommunicationRecordId && record.ProtocolType == context.ProtocolType)
            .Where(record => record.FaultType == context.ObservedFaultType || record.ErrorCode == context.ErrorCode)
            .OrderByDescending(record => record.CreatedAt)
            .Take(maximumCount)
            .Select(record => new RagHistoricalCase(
                record.Id, record.ProtocolType, record.Status, record.FaultType, record.ErrorCode,
                record.ErrorMessage, record.ResponseTimeMs, record.RetryCount, record.CreatedAt))
            .ToListAsync(ct);

    public Task AddTraceAsync(RagDiagnosisTrace trace, CancellationToken ct) => db.RagDiagnosisTraces.AddAsync(trace, ct).AsTask();

    async Task<RagEvaluation?> IRagDiagnosisRepository.GetEvaluationAsync(Guid experimentId, CancellationToken ct)
    {
        var rows = await (from experiment in db.Experiments.AsNoTracking()
                          where experiment.Id == experimentId
                          join record in db.CommunicationRecords.AsNoTracking() on experiment.Id equals record.ExperimentId
                          join diagnosis in db.DiagnosisResults.AsNoTracking() on record.Id equals diagnosis.CommunicationRecordId
                          join trace in db.RagDiagnosisTraces.AsNoTracking() on diagnosis.Id equals trace.DiagnosisResultId
                          where diagnosis.DiagnosisMethod == DiagnosisMethod.Rag
                          select new { experiment.ActualFaultType, diagnosis.PredictedFaultType, diagnosis.DiagnosisDurationMs, trace.CandidateFaultTypes })
            .ToListAsync(ct);

        if (rows.Count == 0 && !await db.Experiments.AnyAsync(experiment => experiment.Id == experimentId, ct)) return null;
        var top1Correct = rows.Count(row => row.ActualFaultType == row.PredictedFaultType);
        var top3Correct = rows.Count(row => DeserializeCandidates(row.CandidateFaultTypes).Contains(row.ActualFaultType));
        return new RagEvaluation(
            rows.Count,
            top1Correct,
            top3Correct,
            rows.Count == 0 ? 0 : decimal.Round((decimal)top1Correct / rows.Count, 4),
            rows.Count == 0 ? 0 : decimal.Round((decimal)top3Correct / rows.Count, 4),
            rows.Count == 0 ? 0 : rows.Average(row => row.DiagnosisDurationMs));
    }

    public async Task<DiagnosisEvaluation?> GetEvaluationAsync(Guid id, CancellationToken ct)
    {
        var rows = await (from experiment in db.Experiments where experiment.Id == id
                          join record in db.CommunicationRecords on experiment.Id equals record.ExperimentId
                          join diagnosis in db.DiagnosisResults on record.Id equals diagnosis.CommunicationRecordId
                          select new { experiment.ActualFaultType, diagnosis.PredictedFaultType }).ToListAsync(ct);
        if (rows.Count == 0 && !await db.Experiments.AnyAsync(experiment => experiment.Id == id, ct)) return null;
        var correct = rows.Count(row => row.ActualFaultType == row.PredictedFaultType);
        return new DiagnosisEvaluation(rows.Count, correct, rows.Count == 0 ? 0 : (decimal)correct / rows.Count);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    private static IReadOnlyList<FaultType> DeserializeCandidates(string candidates)
    {
        try
        {
            return JsonSerializer.Deserialize<FaultType[]>(candidates) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
