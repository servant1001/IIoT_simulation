using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Application.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IIoT.FaultDiagnosis.Worker;

public sealed class CollectorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectorOptions> options,
    TimeProvider timeProvider,
    ILogger<CollectorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var collectorOptions = options.Value;
        collectorOptions.Validate();
        var context = collectorOptions.ToExecutionContext();
        var nextMetricAt = timeProvider.GetUtcNow();
        logger.LogInformation(
            "Collector worker started. PollingIntervalMs={PollingIntervalMs}, MaxRetry={MaxRetry}, MetricIntervalMs={MetricIntervalMs}, ExperimentId={ExperimentId}, ExperimentRunId={ExperimentRunId}",
            collectorOptions.DefaultPollingIntervalMs,
            collectorOptions.MaxRetry,
            collectorOptions.MetricIntervalMs,
            context.ExperimentId,
            context.ExperimentRunId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var collector = scope.ServiceProvider.GetRequiredService<ICollectorService>();
                await collector.CollectEnabledDevicesAsync(context, stoppingToken);

                if (timeProvider.GetUtcNow() >= nextMetricAt)
                {
                    var metricService = scope.ServiceProvider.GetRequiredService<IMetricService>();
                    await metricService.CaptureAsync(context, stoppingToken);
                    nextMetricAt = timeProvider.GetUtcNow().AddMilliseconds(collectorOptions.MetricIntervalMs);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Collector worker iteration failed and will continue on the next polling interval.");
            }

            try
            {
                await Task.Delay(collectorOptions.DefaultPollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Collector worker stopped.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Collector worker is stopping gracefully.");
        await base.StopAsync(cancellationToken);
    }
}
