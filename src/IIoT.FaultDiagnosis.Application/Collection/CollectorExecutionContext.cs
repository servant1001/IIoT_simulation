namespace IIoT.FaultDiagnosis.Application.Collection;

public sealed record CollectorExecutionContext(Guid? ExperimentId, Guid? ExperimentRunId)
{
    public static CollectorExecutionContext None { get; } = new(null, null);

    public void Validate()
    {
        if (ExperimentId.HasValue != ExperimentRunId.HasValue)
        {
            throw new InvalidOperationException("ExperimentId and ExperimentRunId must be set together.");
        }
    }
}
