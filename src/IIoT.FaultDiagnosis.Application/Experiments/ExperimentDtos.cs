using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Experiments;

public sealed record CreateExperimentCommand(string Name, ProtocolType ProtocolType, FaultType ActualFaultType, int DeviceCount, int TagCount, int PollingIntervalMs, int DurationSeconds, int RepeatCount);
public sealed record ExperimentDto(Guid Id, string Name, ProtocolType ProtocolType, FaultType ActualFaultType, int DeviceCount, int TagCount, int PollingIntervalMs, int DurationSeconds, int RepeatCount, ExperimentStatus Status, DateTimeOffset CreatedAt);
public sealed record ExperimentRunDto(Guid Id, int RunNumber, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, int SuccessCount, int FailureCount, double? AverageResponseTimeMs, double? MaxResponseTimeMs, double? MinResponseTimeMs, decimal? CpuAverage, decimal? MemoryAverageMb, ExperimentStatus Status);
public sealed record CommunicationRecordDto(Guid Id, Guid? ExperimentRunId, Guid DeviceId, ProtocolType ProtocolType, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, double? ResponseTimeMs, CommunicationStatus Status, FaultType FaultType, string? ErrorCode, string? ErrorMessage, int RetryCount);
public sealed record ExperimentExport(string FileName, byte[] Content);
