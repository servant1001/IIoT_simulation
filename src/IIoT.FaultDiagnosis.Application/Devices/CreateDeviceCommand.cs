using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Devices;

public sealed record CreateDeviceCommand(
    string Name,
    ProtocolType ProtocolType,
    string Host,
    int Port,
    bool IsEnabled);
