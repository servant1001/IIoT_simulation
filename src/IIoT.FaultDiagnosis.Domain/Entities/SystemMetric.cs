namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class SystemMetric
{
    private SystemMetric()
    {
    }

    public SystemMetric(
        Guid id,
        Guid? experimentRunId,
        DateTimeOffset timestamp,
        decimal cpuPercent,
        decimal memoryMb,
        int? threadCount,
        decimal? gcHeapMb,
        int? handleCount)
    {
        Id = id;
        ExperimentRunId = experimentRunId;
        Timestamp = timestamp;
        CpuPercent = cpuPercent;
        MemoryMb = memoryMb;
        ThreadCount = threadCount;
        GcHeapMb = gcHeapMb;
        HandleCount = handleCount;
    }

    public Guid Id { get; private set; }
    public Guid? ExperimentRunId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public decimal CpuPercent { get; private set; }
    public decimal MemoryMb { get; private set; }
    public int? ThreadCount { get; private set; }
    public decimal? GcHeapMb { get; private set; }
    public int? HandleCount { get; private set; }

    public ExperimentRun? ExperimentRun { get; private set; }
}
