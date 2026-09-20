using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Application.Metrics;

public interface IMetricRepository
{
    Task AddAsync(SystemMetric metric, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
