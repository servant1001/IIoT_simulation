using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Application.Metrics;

public interface IMetricService
{
    Task<SystemMetric> CaptureAsync(CollectorExecutionContext context, CancellationToken cancellationToken);
}
