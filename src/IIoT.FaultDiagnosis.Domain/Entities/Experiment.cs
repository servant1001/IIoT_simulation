using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class Experiment
{
    private Experiment() { }

    public Experiment(Guid id, string name, ProtocolType protocolType, FaultType actualFaultType, int deviceCount, int tagCount, int pollingIntervalMs, int durationSeconds, int repeatCount, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (deviceCount < 1 || tagCount < 1 || pollingIntervalMs < 100 || durationSeconds < 1 || repeatCount < 1) throw new ArgumentOutOfRangeException(nameof(deviceCount));
        Id = id; Name = name.Trim(); ProtocolType = protocolType; ActualFaultType = actualFaultType;
        DeviceCount = deviceCount; TagCount = tagCount; PollingIntervalMs = pollingIntervalMs; DurationSeconds = durationSeconds; RepeatCount = repeatCount;
        CreatedAt = createdAt; Status = ExperimentStatus.Draft;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ProtocolType ProtocolType { get; private set; }
    public FaultType ActualFaultType { get; private set; }
    public int DeviceCount { get; private set; }
    public int TagCount { get; private set; }
    public int PollingIntervalMs { get; private set; }
    public int DurationSeconds { get; private set; }
    public int RepeatCount { get; private set; }
    public ExperimentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<ExperimentRun> Runs { get; } = new List<ExperimentRun>();
    public ICollection<CommunicationRecord> CommunicationRecords { get; } = new List<CommunicationRecord>();

    public void Start() => Status = ExperimentStatus.Running;
    public void Complete() => Status = ExperimentStatus.Completed;
    public void Stop() => Status = ExperimentStatus.Stopped;
    public void Fail() => Status = ExperimentStatus.Failed;
}
