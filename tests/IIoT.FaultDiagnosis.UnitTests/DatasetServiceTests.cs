using System.Text;
using IIoT.FaultDiagnosis.Application.Datasets;
using IIoT.FaultDiagnosis.Domain.Enums;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class DatasetServiceTests
{
    [Fact]
    public async Task ExportAsync_KeepsGroundTruthAndPredictionInSeparateColumns()
    {
        var actual = new DatasetRecord(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), ProtocolType.Mqtt,
            FaultType.BrokerUnavailable, FaultType.ConnectionRefused, DiagnosisMethod.RuleBased, 0.9m,
            DateTimeOffset.Parse("2026-09-20T00:00:00Z"), DateTimeOffset.Parse("2026-09-20T00:00:01Z"), 100,
            CommunicationStatus.Failed, FaultType.ConnectionRefused, "ConnectionRefused", "broker unavailable, retry", 3,
            "request", "{\"temperature\":56.2}", "payload");
        var service = new DatasetService(new MemoryRepository([actual]));

        var export = await service.ExportAsync(new DatasetQuery(), CancellationToken.None);
        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Contains("actual_fault_type,predicted_fault_type", csv);
        Assert.Contains("\"BrokerUnavailable\",\"ConnectionRefused\"", csv);
        Assert.Contains("\"broker unavailable, retry\"", csv);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsDistinctRunsAndDiagnosedRecords()
    {
        var experimentId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var records = new[]
        {
            CreateRecord(experimentId, runId, 1, FaultType.Timeout, FaultType.Timeout),
            CreateRecord(experimentId, runId, 1, FaultType.Timeout, null)
        };
        var service = new DatasetService(new MemoryRepository(records));

        var summary = await service.GetSummaryAsync(100, CancellationToken.None);
        var coverage = Assert.Single(summary.Coverage);

        Assert.Equal(2, summary.RecordCount);
        Assert.Equal(1, summary.ExperimentCount);
        Assert.Equal(1, coverage.RunCount);
        Assert.Equal(2, coverage.RecordCount);
        Assert.Equal(1, coverage.DiagnosedRecordCount);
        Assert.Equal(99, coverage.RemainingRunCount);
    }

    [Fact]
    public async Task ExportAsync_AppliesProtocolGroundTruthAndDiagnosisFilters()
    {
        var selected = CreateRecord(Guid.NewGuid(), Guid.NewGuid(), 1, FaultType.Timeout, FaultType.Timeout);
        var excluded = CreateRecord(Guid.NewGuid(), Guid.NewGuid(), 1, FaultType.BrokerUnavailable, null, ProtocolType.Mqtt);
        var service = new DatasetService(new MemoryRepository([selected, excluded]));

        var export = await service.ExportAsync(new DatasetQuery(ProtocolType.ModbusTcp, FaultType.Timeout, true), CancellationToken.None);
        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Contains(selected.CommunicationRecordId.ToString(), csv);
        Assert.DoesNotContain(excluded.CommunicationRecordId.ToString(), csv);
    }

    private static DatasetRecord CreateRecord(Guid experimentId, Guid runId, int runNumber, FaultType actualFaultType, FaultType? predictedFaultType, ProtocolType protocolType = ProtocolType.ModbusTcp) => new(
        Guid.NewGuid(), experimentId, runId, runNumber, Guid.NewGuid(), protocolType, actualFaultType, predictedFaultType,
        predictedFaultType is null ? null : DiagnosisMethod.RuleBased, predictedFaultType is null ? null : 0.8m,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 10, CommunicationStatus.Failed, actualFaultType,
        null, null, 0, null, null, null);

    private sealed class MemoryRepository(IReadOnlyList<DatasetRecord> records) : IDatasetRepository
    {
        public Task<IReadOnlyList<DatasetRecord>> GetRecordsAsync(CancellationToken cancellationToken) => Task.FromResult(records);
    }
}
