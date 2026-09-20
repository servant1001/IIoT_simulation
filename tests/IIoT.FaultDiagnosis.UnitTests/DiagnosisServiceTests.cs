using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class DiagnosisServiceTests
{
    [Fact]
    public async Task DiagnoseAsync_PersistsRuleBasedPredictionWithoutChangingActualRecord()
    {
        var record = new CommunicationRecord(Guid.NewGuid(), Guid.NewGuid(), ProtocolType.ModbusTcp, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, CommunicationStatus.Failed, FaultType.Timeout, "ReadTimeout", "Timed out", null, null, null, DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(record);
        var engine = new RuleBasedFaultDiagnosisEngine([new FaultTypeRule(FaultType.Timeout, "No response.", "Check latency.")], TimeProvider.System);
        var result = await new DiagnosisService(repository, engine).DiagnoseAsync(record.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(FaultType.Timeout, result!.PredictedFaultType);
        Assert.Equal(FaultType.Timeout, record.FaultType);
        Assert.Same(result, Assert.Single(repository.Results));
    }

    private sealed class MemoryRepository(CommunicationRecord record) : IDiagnosisRepository
    {
        public List<DiagnosisResult> Results { get; } = [];
        public Task<CommunicationRecord?> GetRecordAsync(Guid id, CancellationToken ct) => Task.FromResult<CommunicationRecord?>(id == record.Id ? record : null);
        public Task AddAsync(DiagnosisResult result, CancellationToken ct) { Results.Add(result); return Task.CompletedTask; }
        public Task<DiagnosisEvaluation?> GetEvaluationAsync(Guid experimentId, CancellationToken ct) => Task.FromResult<DiagnosisEvaluation?>(null);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
