using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Application.Experiments;

public interface IExperimentRepository
{
    Task AddAsync(Experiment experiment, CancellationToken cancellationToken);
    Task<Experiment?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExperimentRun>> GetRunsAsync(Guid experimentId, CancellationToken cancellationToken);
    Task AddRunAsync(ExperimentRun run, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunicationRecord>> GetRecordsAsync(Guid experimentId, CancellationToken cancellationToken);
    Task<ExperimentRunStatistics> GetStatisticsAsync(Guid runId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record ExperimentRunStatistics(int SuccessCount, int FailureCount, double? AverageResponseTimeMs, double? MaxResponseTimeMs, double? MinResponseTimeMs, decimal? CpuAverage, decimal? MemoryAverageMb);
