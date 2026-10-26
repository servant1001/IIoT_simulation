using System.Diagnostics;
using System.Text.Json;
using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Rag;

public sealed class RagDiagnosisService(
    IRagDiagnosisRepository ragRepository,
    IDiagnosisRepository diagnosisRepository,
    IRagKnowledgeBase knowledgeBase,
    TimeProvider timeProvider) : IRagDiagnosisService
{
    private const int HistoricalCaseLimit = 3;
    private const int KnowledgeArticleLimit = 3;

    public async Task<RagDiagnosisResponse?> DiagnoseAsync(Guid communicationRecordId, CancellationToken cancellationToken)
    {
        var context = await ragRepository.GetContextAsync(communicationRecordId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        var articles = knowledgeBase.Search(context, KnowledgeArticleLimit);
        var historicalCases = await ragRepository.GetHistoricalCasesAsync(context, HistoricalCaseLimit, cancellationToken);
        var primary = articles.FirstOrDefault();
        var predictedFaultType = primary.Article?.FaultType ?? FallbackFaultType(context);
        var possibleCauses = primary.Article?.PossibleCauses ?? DefaultCauses(context);
        var suggestedActions = primary.Article?.SuggestedActions ?? DefaultActions(context);
        var candidateFaultTypes = articles.Select(item => item.Article.FaultType).Distinct().ToList();
        if (!candidateFaultTypes.Contains(predictedFaultType))
        {
            candidateFaultTypes.Insert(0, predictedFaultType);
        }

        stopwatch.Stop();
        var confidence = CalculateConfidence(primary.Score, historicalCases.Count);
        var result = new DiagnosisResult(
            Guid.NewGuid(),
            communicationRecordId,
            DiagnosisMethod.Rag,
            predictedFaultType,
            confidence,
            JsonSerializer.Serialize(possibleCauses),
            JsonSerializer.Serialize(suggestedActions),
            stopwatch.Elapsed.TotalMilliseconds,
            timeProvider.GetUtcNow());
        await diagnosisRepository.AddAsync(result, cancellationToken);
        await ragRepository.AddTraceAsync(new RagDiagnosisTrace(
            Guid.NewGuid(),
            result.Id,
            JsonSerializer.Serialize(candidateFaultTypes),
            JsonSerializer.Serialize(articles.Select(item => item.Article.Id)),
            JsonSerializer.Serialize(historicalCases.Select(item => item.CommunicationRecordId)),
            "LocalKnowledgeBase",
            timeProvider.GetUtcNow()), cancellationToken);
        await diagnosisRepository.SaveChangesAsync(cancellationToken);

        return new RagDiagnosisResponse(
            communicationRecordId,
            predictedFaultType,
            candidateFaultTypes,
            possibleCauses,
            suggestedActions,
            confidence,
            stopwatch.Elapsed.TotalMilliseconds,
            articles.Select(item => new RagSource(item.Article.Id, item.Article.Title, item.Article.FaultType, item.Score)).ToArray(),
            historicalCases);
    }

    public Task<RagEvaluation?> GetEvaluationAsync(Guid experimentId, CancellationToken cancellationToken) =>
        ragRepository.GetEvaluationAsync(experimentId, cancellationToken);

    private static FaultType FallbackFaultType(RagRecordContext context) =>
        context.ObservedFaultType is FaultType.None or FaultType.Unknown ? FaultType.Unknown : context.ObservedFaultType;

    private static IReadOnlyList<string> DefaultCauses(RagRecordContext context) =>
    [
        $"The {context.ProtocolType} communication record did not match a curated knowledge article.",
        "The error code and message should be reviewed with the device and protocol configuration."
    ];

    private static IReadOnlyList<string> DefaultActions(RagRecordContext context) =>
    [
        $"Confirm the configured endpoint {context.DeviceHost}:{context.DevicePort}.",
        "Capture a reproducible communication trace before changing retry or timeout settings."
    ];

    private static decimal CalculateConfidence(int knowledgeScore, int historicalCaseCount)
    {
        var score = 0.35m + Math.Min(0.50m, knowledgeScore / 40m) + Math.Min(0.10m, historicalCaseCount * 0.03m);
        return decimal.Round(Math.Min(score, 0.95m), 4);
    }
}
