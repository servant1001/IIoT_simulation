using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Application.Rag;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class RagDiagnosisServiceTests
{
    [Fact]
    public async Task DiagnoseAsync_UsesRetrievedKnowledgeAndPersistsRagPrediction()
    {
        var recordId = Guid.NewGuid();
        var context = new RagRecordContext(recordId, ProtocolType.ModbusTcp, "127.0.0.1", 502, CommunicationStatus.Failed,
            FaultType.IllegalAddress, "IllegalDataAddress", "Modbus exception code 2: illegal address", 18, 1);
        var repository = new MemoryRepository(context,
        [
            new RagHistoricalCase(Guid.NewGuid(), ProtocolType.ModbusTcp, CommunicationStatus.Failed, FaultType.IllegalAddress,
                "IllegalDataAddress", "Illegal address", 20, 1, DateTimeOffset.UtcNow)
        ]);
        var service = new RagDiagnosisService(repository, repository, new RagKnowledgeBase(), TimeProvider.System);

        var result = await service.DiagnoseAsync(recordId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(FaultType.IllegalAddress, result!.PredictedFaultType);
        Assert.Contains(FaultType.IllegalAddress, result.CandidateFaultTypes);
        Assert.Contains(result.Sources, source => source.Id == "modbus-illegal-address");
        var saved = Assert.Single(repository.SavedResults);
        Assert.Equal(DiagnosisMethod.Rag, saved.DiagnosisMethod);
        Assert.Equal(FaultType.IllegalAddress, saved.PredictedFaultType);
        Assert.Equal(saved.Id, Assert.Single(repository.SavedTraces).DiagnosisResultId);
    }

    [Fact]
    public async Task DiagnoseAsync_ReturnsNullWhenCommunicationRecordDoesNotExist()
    {
        var repository = new MemoryRepository(null, []);
        var service = new RagDiagnosisService(repository, repository, new RagKnowledgeBase(), TimeProvider.System);

        var result = await service.DiagnoseAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(repository.SavedResults);
    }

    [Fact]
    public void Search_DoesNotRequireExperimentGroundTruth()
    {
        var context = new RagRecordContext(Guid.NewGuid(), ProtocolType.Mqtt, "127.0.0.1", 1883, CommunicationStatus.Failed,
            FaultType.AuthenticationFailed, "NotAuthorized", "Connection not authorized", 5, 0);

        var result = new RagKnowledgeBase().Search(context, 3);

        Assert.Equal(FaultType.AuthenticationFailed, result.First().Article.FaultType);
    }

    private sealed class MemoryRepository(RagRecordContext? context, IReadOnlyList<RagHistoricalCase> historicalCases) : IRagDiagnosisRepository, IDiagnosisRepository
    {
        public List<DiagnosisResult> SavedResults { get; } = [];
        public List<RagDiagnosisTrace> SavedTraces { get; } = [];
        public Task<RagRecordContext?> GetContextAsync(Guid communicationRecordId, CancellationToken cancellationToken) =>
            Task.FromResult(context?.CommunicationRecordId == communicationRecordId ? context : null);
        public Task<IReadOnlyList<RagHistoricalCase>> GetHistoricalCasesAsync(RagRecordContext input, int maximumCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RagHistoricalCase>>(historicalCases.Take(maximumCount).ToArray());
        public Task AddTraceAsync(RagDiagnosisTrace trace, CancellationToken cancellationToken) { SavedTraces.Add(trace); return Task.CompletedTask; }
        Task<RagEvaluation?> IRagDiagnosisRepository.GetEvaluationAsync(Guid experimentId, CancellationToken cancellationToken) => Task.FromResult<RagEvaluation?>(null);
        public Task<CommunicationRecord?> GetRecordAsync(Guid id, CancellationToken ct) => Task.FromResult<CommunicationRecord?>(null);
        public Task AddAsync(DiagnosisResult result, CancellationToken ct) { SavedResults.Add(result); return Task.CompletedTask; }
        public Task<DiagnosisEvaluation?> GetEvaluationAsync(Guid experimentId, CancellationToken ct) => Task.FromResult<DiagnosisEvaluation?>(null);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
