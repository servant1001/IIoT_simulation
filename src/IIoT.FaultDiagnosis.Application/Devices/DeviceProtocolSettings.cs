using System.Text.Json;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using IIoT.FaultDiagnosis.Protocols.Mqtt;
using IIoT.FaultDiagnosis.Protocols.OpcUa;

namespace IIoT.FaultDiagnosis.Application.Devices;

public sealed record DeviceProtocolSettings(
    int ModbusSlaveId = 1,
    int ModbusStartAddress = 0,
    int ModbusNumberOfPoints = 6,
    IReadOnlyList<string>? OpcUaNodeIds = null,
    bool OpcUaUseSecurity = false,
    int OpcUaOperationTimeoutMs = 10000,
    string MqttTopic = "factory/machine01/data",
    int MqttQualityOfService = 1,
    int MqttReceiveTimeoutMs = 10000,
    int MqttReconnectDelayMs = 1000,
    bool MqttRequireJsonPayload = true,
    string? MqttUsername = null,
    string? MqttPassword = null)
{
    public static DeviceProtocolSettings Default { get; } = new();

    public IReadOnlyList<string> EffectiveOpcUaNodeIds =>
        OpcUaNodeIds is { Count: > 0 } ? OpcUaNodeIds : ["i=2258"];

    public void ValidateFor(ProtocolType protocolType)
    {
        switch (protocolType)
        {
            case ProtocolType.ModbusTcp:
                ToModbusConfiguration().Validate();
                break;
            case ProtocolType.OpcUa:
                new OpcUaConfiguration("opc.tcp://placeholder:4840", EffectiveOpcUaNodeIds, UseSecurity: OpcUaUseSecurity, OperationTimeoutMs: OpcUaOperationTimeoutMs).Validate();
                break;
            case ProtocolType.Mqtt:
                ToMqttConfiguration().Validate();
                break;
        }
    }

    public ModbusConfiguration ToModbusConfiguration() => new(
        checked((byte)ModbusSlaveId),
        checked((ushort)ModbusStartAddress),
        checked((ushort)ModbusNumberOfPoints));

    public OpcUaConfiguration ToOpcUaConfiguration(string endpoint) => new(
        endpoint,
        EffectiveOpcUaNodeIds,
        UseSecurity: OpcUaUseSecurity,
        OperationTimeoutMs: OpcUaOperationTimeoutMs);

    public MqttConfiguration ToMqttConfiguration() => new(
        MqttTopic,
        MqttQualityOfService,
        MqttReceiveTimeoutMs,
        MqttReconnectDelayMs,
        MqttRequireJsonPayload,
        MqttUsername,
        MqttPassword);

    public DeviceProtocolSettings WithoutSecrets() => this with { MqttPassword = null };

    public static DeviceProtocolSettings Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Default;

        try
        {
            return JsonSerializer.Deserialize<DeviceProtocolSettings>(json) ?? Default;
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Protocol settings must be valid JSON.", nameof(json), exception);
        }
    }

    public string Serialize() => JsonSerializer.Serialize(this);
}
