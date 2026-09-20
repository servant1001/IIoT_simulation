using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IIoT.FaultDiagnosis.Application.Metrics;

public sealed class MetricService(
    IMetricRepository metricRepository,
    ProcessMetricsSampler sampler,
    ILogger<MetricService> logger) : IMetricService
{
    public async Task<SystemMetric> CaptureAsync(CollectorExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Validate();
        var snapshot = sampler.Capture();
        var metric = new SystemMetric(
            Guid.NewGuid(),
            context.ExperimentRunId,
            snapshot.Timestamp,
            snapshot.CpuPercent,
            snapshot.MemoryMb,
            snapshot.ThreadCount,
            snapshot.GcHeapMb,
            snapshot.HandleCount);

        await metricRepository.AddAsync(metric, cancellationToken);
        await metricRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "System metric captured. ExperimentId={ExperimentId}, ExperimentRunId={ExperimentRunId}, CpuPercent={CpuPercent}, MemoryMb={MemoryMb}",
            context.ExperimentId,
            context.ExperimentRunId,
            metric.CpuPercent,
            metric.MemoryMb);
        return metric;
    }
}
