using IIoT.FaultDiagnosis.Application.Metrics;
using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class MetricRepository(FaultDiagnosisDbContext dbContext) : IMetricRepository
{
    public Task AddAsync(SystemMetric metric, CancellationToken cancellationToken) =>
        dbContext.SystemMetrics.AddAsync(metric, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
