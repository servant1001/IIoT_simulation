using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Application.Metrics;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IIoT.FaultDiagnosis.Application.Experiments;

public sealed class ExperimentService(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<ExperimentService> logger) : IExperimentService
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> executions = new();

    public async Task<ExperimentDto> CreateAsync(CreateExperimentCommand command, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();
        var experiment = new Experiment(Guid.NewGuid(), command.Name, command.ProtocolType, command.ActualFaultType, command.DeviceCount, command.TagCount, command.PollingIntervalMs, command.DurationSeconds, command.RepeatCount, timeProvider.GetUtcNow());
        await repository.AddAsync(experiment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(experiment);
    }

    public async Task<ExperimentDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var experiment = await scope.ServiceProvider.GetRequiredService<IExperimentRepository>().GetAsync(id, cancellationToken);
        return experiment is null ? null : Map(experiment);
    }

    public async Task<IReadOnlyList<ExperimentRunDto>?> GetRunsAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();
        if (await repository.GetAsync(id, cancellationToken) is null) return null;
        return (await repository.GetRunsAsync(id, cancellationToken)).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<CommunicationRecordDto>?> GetRecordsAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();
        if (await repository.GetAsync(id, cancellationToken) is null) return null;
        return (await repository.GetRecordsAsync(id, cancellationToken)).Select(Map).ToList();
    }

    public async Task<ExperimentExport?> ExportAsync(Guid id, CancellationToken cancellationToken)
    {
        var records = await GetRecordsAsync(id, cancellationToken);
        if (records is null) return null;
        var builder = new StringBuilder("experiment_id,run_id,device_id,protocol,timestamp,response_time_ms,status,fault_type,error_code,error_message,retry_count\r\n");
        foreach (var record in records)
            builder.Append(string.Join(',', Escape(id), Escape(record.ExperimentRunId), Escape(record.DeviceId), Escape(record.ProtocolType), Escape(record.StartedAt.ToString("O")), Escape(record.ResponseTimeMs?.ToString(CultureInfo.InvariantCulture)), Escape(record.Status), Escape(record.FaultType), Escape(record.ErrorCode), Escape(record.ErrorMessage), Escape(record.RetryCount))).Append("\r\n");
        return new ExperimentExport($"experiment-{id:N}.csv", Encoding.UTF8.GetBytes(builder.ToString()));
    }

    public async Task<bool> StartAsync(Guid id, CancellationToken cancellationToken)
    {
        if (executions.ContainsKey(id)) return false;
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();
        var experiment = await repository.GetAsync(id, cancellationToken);
        if (experiment is null || experiment.Status != ExperimentStatus.Draft) return false;
        experiment.Start();
        await repository.SaveChangesAsync(cancellationToken);
        var source = new CancellationTokenSource();
        if (!executions.TryAdd(id, source)) { source.Dispose(); return false; }
        _ = Task.Run(() => ExecuteAsync(id, source), CancellationToken.None);
        return true;
    }

    public async Task<bool> StopAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!executions.TryGetValue(id, out var source)) return false;
        source.Cancel();
        await Task.CompletedTask;
        return true;
    }

    private async Task ExecuteAsync(Guid experimentId, CancellationTokenSource source)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();
            var collector = scope.ServiceProvider.GetRequiredService<ICollectorService>();
            var metricService = scope.ServiceProvider.GetRequiredService<IMetricService>();
            var experiment = await repository.GetAsync(experimentId, source.Token);
            if (experiment is null) return;
            for (var number = 1; number <= experiment.RepeatCount && !source.IsCancellationRequested; number++)
            {
                var run = new ExperimentRun(Guid.NewGuid(), experiment.Id, number, timeProvider.GetUtcNow());
                await repository.AddRunAsync(run, source.Token);
                await repository.SaveChangesAsync(source.Token);
                var context = new CollectorExecutionContext(experiment.Id, run.Id);
                var until = timeProvider.GetUtcNow().AddSeconds(experiment.DurationSeconds);
                var nextMetricAt = timeProvider.GetUtcNow();
                try
                {
                    while (timeProvider.GetUtcNow() < until && !source.IsCancellationRequested)
                    {
                        await collector.CollectEnabledDevicesAsync(context, source.Token);
                        if (timeProvider.GetUtcNow() >= nextMetricAt)
                        {
                            await metricService.CaptureAsync(context, source.Token);
                            nextMetricAt = timeProvider.GetUtcNow().AddSeconds(5);
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(experiment.PollingIntervalMs), source.Token);
                    }
                }
                catch (OperationCanceledException) when (source.IsCancellationRequested) { }
                var statistics = await repository.GetStatisticsAsync(run.Id, CancellationToken.None);
                run.Complete(statistics.SuccessCount, statistics.FailureCount, statistics.AverageResponseTimeMs, statistics.MaxResponseTimeMs, statistics.MinResponseTimeMs, statistics.CpuAverage, statistics.MemoryAverageMb, timeProvider.GetUtcNow(), source.IsCancellationRequested ? ExperimentStatus.Stopped : ExperimentStatus.Completed);
                await repository.SaveChangesAsync(CancellationToken.None);
            }
            if (source.IsCancellationRequested) experiment.Stop(); else experiment.Complete();
            await repository.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Experiment execution failed. ExperimentId={ExperimentId}", experimentId);
            await MarkFailedAsync(experimentId);
        }
        finally
        {
            if (executions.TryRemove(experimentId, out var removed)) removed.Dispose();
        }
    }

    private async Task MarkFailedAsync(Guid id)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();
        var experiment = await repository.GetAsync(id, CancellationToken.None);
        if (experiment is null) return;
        experiment.Fail();
        await repository.SaveChangesAsync(CancellationToken.None);
    }

    private static ExperimentDto Map(Experiment item) => new(item.Id, item.Name, item.ProtocolType, item.ActualFaultType, item.DeviceCount, item.TagCount, item.PollingIntervalMs, item.DurationSeconds, item.RepeatCount, item.Status, item.CreatedAt);
    private static ExperimentRunDto Map(ExperimentRun item) => new(item.Id, item.RunNumber, item.StartedAt, item.CompletedAt, item.SuccessCount, item.FailureCount, item.AverageResponseTimeMs, item.MaxResponseTimeMs, item.MinResponseTimeMs, item.CpuAverage, item.MemoryAverageMb, item.Status);
    private static CommunicationRecordDto Map(CommunicationRecord item) => new(item.Id, item.ExperimentRunId, item.DeviceId, item.ProtocolType, item.StartedAt, item.CompletedAt, item.ResponseTimeMs, item.Status, item.FaultType, item.ErrorCode, item.ErrorMessage, item.RetryCount);
    private static string Escape(object? value) => $"\"{(value?.ToString() ?? string.Empty).Replace("\"", "\"\"")}\"";
}
