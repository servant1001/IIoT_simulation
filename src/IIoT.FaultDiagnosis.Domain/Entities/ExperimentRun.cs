using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class ExperimentRun
{
    private ExperimentRun() { }

    public ExperimentRun(Guid id, Guid experimentId, int runNumber, DateTimeOffset startedAt)
    {
        Id = id; ExperimentId = experimentId; RunNumber = runNumber; StartedAt = startedAt; Status = ExperimentStatus.Running;
    }
    public Guid Id { get; private set; }
    public Guid ExperimentId { get; private set; }
    public int RunNumber { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public double? AverageResponseTimeMs { get; private set; }
    public double? MaxResponseTimeMs { get; private set; }
    public double? MinResponseTimeMs { get; private set; }
    public decimal? CpuAverage { get; private set; }
    public decimal? MemoryAverageMb { get; private set; }
    public ExperimentStatus Status { get; private set; }

    public Experiment Experiment { get; private set; } = null!;
    public ICollection<CommunicationRecord> CommunicationRecords { get; } = new List<CommunicationRecord>();
    public ICollection<SystemMetric> SystemMetrics { get; } = new List<SystemMetric>();

    public void Complete(int successCount, int failureCount, double? averageResponseTimeMs, double? maxResponseTimeMs, double? minResponseTimeMs, decimal? cpuAverage, decimal? memoryAverageMb, DateTimeOffset completedAt, ExperimentStatus status)
    {
        SuccessCount = successCount; FailureCount = failureCount; AverageResponseTimeMs = averageResponseTimeMs; MaxResponseTimeMs = maxResponseTimeMs; MinResponseTimeMs = minResponseTimeMs;
        CpuAverage = cpuAverage; MemoryAverageMb = memoryAverageMb; CompletedAt = completedAt; Status = status;
    }
}
