using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Xunit;
namespace IIoT.FaultDiagnosis.UnitTests;
public sealed class RuleBasedDiagnosisTests
{
 [Theory]
 [InlineData(FaultType.ConnectionRefused)]
 [InlineData(FaultType.Timeout)]
 [InlineData(FaultType.IllegalAddress)]
 [InlineData(FaultType.None)]
 [InlineData(FaultType.InvalidEndpoint)]
 [InlineData(FaultType.InvalidNodeId)]
 [InlineData(FaultType.CertificateError)]
 [InlineData(FaultType.SessionDisconnected)]
 [InlineData(FaultType.BrokerUnavailable)]
 [InlineData(FaultType.AuthenticationFailed)]
 [InlineData(FaultType.TopicMismatch)]
 [InlineData(FaultType.PayloadInvalid)]
 [InlineData(FaultType.Unknown)]
 public async Task DiagnoseAsync_UsesMatchingRule(FaultType faultType)
 {
  var record=new CommunicationRecord(Guid.NewGuid(),Guid.NewGuid(),ProtocolType.ModbusTcp,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,1,CommunicationStatus.Failed,faultType,null,null,null,null,null,DateTimeOffset.UtcNow);
  var engine=new RuleBasedFaultDiagnosisEngine([new FaultTypeRule(faultType,"c","a")],TimeProvider.System);
  var result=await engine.DiagnoseAsync(record,CancellationToken.None);
  Assert.Equal(faultType,result.PredictedFaultType); Assert.Equal(DiagnosisMethod.RuleBased,result.DiagnosisMethod);
 }

 [Fact]
 public async Task DiagnoseAsync_ClassifiesSuccessfulSlowModbusResponseAsNetworkDelay()
 {
  var record=new CommunicationRecord(Guid.NewGuid(),Guid.NewGuid(),ProtocolType.ModbusTcp,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,150,CommunicationStatus.Success,FaultType.None,null,null,null,null,null,DateTimeOffset.UtcNow);
  var engine=new RuleBasedFaultDiagnosisEngine([new NetworkDelayRule(ProtocolType.ModbusTcp,100,"c","a"),new FaultTypeRule(FaultType.None,"c","a")],TimeProvider.System);
  var result=await engine.DiagnoseAsync(record,CancellationToken.None);
  Assert.Equal(FaultType.NetworkDelay,result.PredictedFaultType);
 }
}
