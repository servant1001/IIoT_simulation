using IIoT.FaultDiagnosis.Application.Experiments;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class ExperimentRepository(FaultDiagnosisDbContext dbContext) : IExperimentRepository
{
    public Task AddAsync(Experiment experiment, CancellationToken cancellationToken) => dbContext.Experiments.AddAsync(experiment, cancellationToken).AsTask();
    public Task<Experiment?> GetAsync(Guid id, CancellationToken cancellationToken) => dbContext.Experiments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<ExperimentRun>> GetRunsAsync(Guid experimentId, CancellationToken cancellationToken) => await dbContext.ExperimentRuns.Where(x => x.ExperimentId == experimentId).OrderBy(x => x.RunNumber).ToListAsync(cancellationToken);
    public Task AddRunAsync(ExperimentRun run, CancellationToken cancellationToken) => dbContext.ExperimentRuns.AddAsync(run, cancellationToken).AsTask();
    public async Task<IReadOnlyList<CommunicationRecord>> GetRecordsAsync(Guid experimentId, CancellationToken cancellationToken) => await dbContext.CommunicationRecords.Where(x => x.ExperimentId == experimentId).OrderBy(x => x.StartedAt).ToListAsync(cancellationToken);
    public async Task<ExperimentRunStatistics> GetStatisticsAsync(Guid runId, CancellationToken cancellationToken)
    {
        var records = dbContext.CommunicationRecords.Where(x => x.ExperimentRunId == runId);
        var successes = await records.CountAsync(x => x.Status == CommunicationStatus.Success, cancellationToken);
        var failures = await records.CountAsync(x => x.Status != CommunicationStatus.Success, cancellationToken);
        var timings = records.Where(x => x.ResponseTimeMs.HasValue).Select(x => x.ResponseTimeMs!.Value);
        var metrics = dbContext.SystemMetrics.Where(x => x.ExperimentRunId == runId);
        return new ExperimentRunStatistics(successes, failures,
            await timings.Select(x => (double?)x).AverageAsync(cancellationToken),
            await timings.Select(x => (double?)x).MaxAsync(cancellationToken),
            await timings.Select(x => (double?)x).MinAsync(cancellationToken),
            await metrics.Select(x => (decimal?)x.CpuPercent).AverageAsync(cancellationToken),
            await metrics.Select(x => (decimal?)x.MemoryMb).AverageAsync(cancellationToken));
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
