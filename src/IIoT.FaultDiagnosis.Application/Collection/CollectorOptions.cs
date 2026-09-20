namespace IIoT.FaultDiagnosis.Application.Collection;

public sealed class CollectorOptions
{
    public const string SectionName = "Collector";

    public int DefaultPollingIntervalMs { get; init; } = 1000;
    public int MaxRetry { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 1000;
    public int MetricIntervalMs { get; init; } = 5000;
    public Guid? ExperimentId { get; init; }
    public Guid? ExperimentRunId { get; init; }

    public void Validate()
    {
        if (DefaultPollingIntervalMs < 100)
        {
            throw new InvalidOperationException("Collector:DefaultPollingIntervalMs must be at least 100.");
        }

        if (MaxRetry is < 0 or > 10)
        {
            throw new InvalidOperationException("Collector:MaxRetry must be between 0 and 10.");
        }

        if (RetryDelayMs < 0)
        {
            throw new InvalidOperationException("Collector:RetryDelayMs cannot be negative.");
        }

        if (MetricIntervalMs < 100)
        {
            throw new InvalidOperationException("Collector:MetricIntervalMs must be at least 100.");
        }

        ToExecutionContext().Validate();
    }

    public CollectorExecutionContext ToExecutionContext() => new(ExperimentId, ExperimentRunId);
}
