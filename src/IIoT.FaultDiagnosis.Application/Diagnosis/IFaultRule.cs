using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
namespace IIoT.FaultDiagnosis.Application.Diagnosis;
public interface IFaultRule { bool Matches(CommunicationRecord record); FaultType PredictedFaultType { get; } string Cause { get; } string Action { get; } }
