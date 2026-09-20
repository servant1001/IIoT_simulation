using System.Diagnostics;
using System.Text.Json;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
namespace IIoT.FaultDiagnosis.Application.Diagnosis;
public interface IFaultDiagnosisEngine { Task<DiagnosisResult> DiagnoseAsync(CommunicationRecord record, CancellationToken cancellationToken); }
public sealed class FaultTypeRule(FaultType faultType, string cause, string action) : IFaultRule { public FaultType PredictedFaultType => faultType; public string Cause => cause; public string Action => action; public bool Matches(CommunicationRecord record) => record.FaultType == faultType; }
public sealed class RuleBasedFaultDiagnosisEngine(IEnumerable<IFaultRule> rules, TimeProvider timeProvider) : IFaultDiagnosisEngine
{ public Task<DiagnosisResult> DiagnoseAsync(CommunicationRecord record, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); var watch=Stopwatch.StartNew(); var rule=rules.FirstOrDefault(x=>x.Matches(record)); watch.Stop(); return Task.FromResult(new DiagnosisResult(Guid.NewGuid(),record.Id,DiagnosisMethod.RuleBased,rule?.PredictedFaultType??FaultType.Unknown,rule is null?0.5m:1m,JsonSerializer.Serialize(new[]{rule?.Cause??"No matching rule."}),JsonSerializer.Serialize(new[]{rule?.Action??"Inspect the raw communication record."}),watch.Elapsed.TotalMilliseconds,timeProvider.GetUtcNow())); } }
