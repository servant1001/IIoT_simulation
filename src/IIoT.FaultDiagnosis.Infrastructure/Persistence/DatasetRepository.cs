using IIoT.FaultDiagnosis.Application.Datasets;
using Microsoft.EntityFrameworkCore;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class DatasetRepository(FaultDiagnosisDbContext dbContext) : IDatasetRepository
{
    public async Task<IReadOnlyList<DatasetRecord>> GetRecordsAsync(CancellationToken cancellationToken)
    {
        var records = await dbContext.CommunicationRecords
            .AsNoTracking()
            .Where(record => record.ExperimentId != null)
            .Include(record => record.Experiment)
            .Include(record => record.ExperimentRun)
            .Include(record => record.DiagnosisResults)
            .OrderBy(record => record.StartedAt)
            .ToListAsync(cancellationToken);

        return records.Select(record =>
        {
            var diagnosis = record.DiagnosisResults.OrderByDescending(result => result.CreatedAt).FirstOrDefault();
            return new DatasetRecord(
                record.Id,
                record.ExperimentId!.Value,
                record.ExperimentRunId,
                record.ExperimentRun?.RunNumber,
                record.DeviceId,
                record.ProtocolType,
                record.Experiment!.ActualFaultType,
                diagnosis?.PredictedFaultType,
                diagnosis?.DiagnosisMethod,
                diagnosis?.Confidence,
                record.StartedAt,
                record.CompletedAt,
                record.ResponseTimeMs,
                record.Status,
                record.FaultType,
                record.ErrorCode,
                record.ErrorMessage,
                record.RetryCount,
                record.RawRequest,
                record.RawResponse,
                record.ParsedValue);
        }).ToList();
    }
}
