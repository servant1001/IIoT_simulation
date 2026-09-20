using System.Diagnostics;

namespace IIoT.FaultDiagnosis.Application.Metrics;

public sealed class ProcessMetricsSampler(TimeProvider timeProvider)
{
    private readonly object _sync = new();
    private DateTimeOffset? _previousTimestamp;
    private TimeSpan? _previousProcessorTime;

    public ProcessMetricSnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();
        var timestamp = timeProvider.GetUtcNow();
        var processorTime = process.TotalProcessorTime;
        decimal cpuPercent;

        lock (_sync)
        {
            cpuPercent = _previousTimestamp is { } previousTimestamp && _previousProcessorTime is { } previousProcessorTime
                ? CalculateCpuPercent(timestamp - previousTimestamp, processorTime - previousProcessorTime)
                : 0m;
            _previousTimestamp = timestamp;
            _previousProcessorTime = processorTime;
        }

        return new ProcessMetricSnapshot(
            timestamp,
            cpuPercent,
            ToMegabytes(process.WorkingSet64),
            process.Threads.Count,
            ToMegabytes(GC.GetTotalMemory(forceFullCollection: false)),
            TryGetHandleCount(process));
    }

    private static decimal CalculateCpuPercent(TimeSpan elapsed, TimeSpan processorTime)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0m;
        }

        var percent = processorTime.TotalMilliseconds / (elapsed.TotalMilliseconds * Environment.ProcessorCount) * 100d;
        return decimal.Round((decimal)Math.Clamp(percent, 0d, 100d), 2);
    }

    private static decimal ToMegabytes(long bytes) => decimal.Round(bytes / 1024m / 1024m, 2);

    private static int? TryGetHandleCount(Process process)
    {
        try
        {
            return process.HandleCount;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }
}

public sealed record ProcessMetricSnapshot(
    DateTimeOffset Timestamp,
    decimal CpuPercent,
    decimal MemoryMb,
    int ThreadCount,
    decimal GcHeapMb,
    int? HandleCount);
