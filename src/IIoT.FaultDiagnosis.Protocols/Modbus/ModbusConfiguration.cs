namespace IIoT.FaultDiagnosis.Protocols.Modbus;

public sealed record ModbusConfiguration(byte SlaveId, ushort StartAddress, ushort NumberOfPoints)
{
    public static ModbusConfiguration Default { get; } = new(1, 0, 6);

    public void Validate()
    {
        if (SlaveId is < 1 or > 247)
        {
            throw new ArgumentOutOfRangeException(nameof(SlaveId), "Slave ID must be between 1 and 247.");
        }

        if (NumberOfPoints is < 1 or > 125)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfPoints), "Holding register count must be between 1 and 125.");
        }
    }
}
