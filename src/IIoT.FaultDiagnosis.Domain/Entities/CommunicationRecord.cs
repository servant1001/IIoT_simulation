using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class CommunicationRecord
{
    private CommunicationRecord()
    {
    }

    public CommunicationRecord(
        Guid id,
        Guid deviceId,
        ProtocolType protocolType,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        double responseTimeMs,
        CommunicationStatus status,
        FaultType faultType,
        string? errorCode,
        string? errorMessage,
        string? rawRequest,
        string? rawResponse,
        string? parsedValue,
        DateTimeOffset createdAt,
        Guid? experimentId = null,
        Guid? experimentRunId = null,
        int retryCount = 0)
    {
        Id = id;
        DeviceId = deviceId;
        ProtocolType = protocolType;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        ResponseTimeMs = responseTimeMs;
        Status = status;
        FaultType = faultType;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RawRequest = rawRequest;
        RawResponse = rawResponse;
        ParsedValue = parsedValue;
        CreatedAt = createdAt;
        ExperimentId = experimentId;
        ExperimentRunId = experimentRunId;
        RetryCount = retryCount;
    }

    public Guid Id { get; private set; }
    public Guid? ExperimentId { get; private set; }
    public Guid? ExperimentRunId { get; private set; }
    public Guid DeviceId { get; private set; }
    public ProtocolType ProtocolType { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public double? ResponseTimeMs { get; private set; }
    public CommunicationStatus Status { get; private set; }
    public FaultType FaultType { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public string? RawRequest { get; private set; }
    public string? RawResponse { get; private set; }
    public string? ParsedValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Experiment? Experiment { get; private set; }
    public ExperimentRun? ExperimentRun { get; private set; }
    public Device Device { get; private set; } = null!;
    public ICollection<DiagnosisResult> DiagnosisResults { get; } = new List<DiagnosisResult>();
}
