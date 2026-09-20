using System.Net.Sockets;
using IIoT.FaultDiagnosis.Domain.Enums;
using NModbus;

namespace IIoT.FaultDiagnosis.Protocols.Modbus;

public sealed record ModbusFault(FaultType FaultType, string ErrorCode);

public static class ModbusFaultNormalizer
{
    public static ModbusFault Normalize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var root = Unwrap(exception);
        return root switch
        {
            SlaveException slaveException => FromSlaveException(slaveException),
            SocketException socketException => FromSocketException(socketException),
            TimeoutException => new ModbusFault(FaultType.Timeout, "Timeout"),
            IOException { InnerException: SocketException socketException } => FromSocketException(socketException),
            IOException => new ModbusFault(FaultType.ConnectionLost, "IOException"),
            _ => new ModbusFault(FaultType.Unknown, root.GetType().Name)
        };
    }

    private static Exception Unwrap(Exception exception) =>
        exception is AggregateException { InnerExceptions.Count: 1 } aggregate
            ? Unwrap(aggregate.InnerException!)
            : exception;

    private static ModbusFault FromSlaveException(SlaveException exception) =>
        exception.SlaveExceptionCode switch
        {
            SlaveExceptionCodes.IllegalDataAddress => new ModbusFault(FaultType.IllegalAddress, "ModbusExceptionCode02"),
            SlaveExceptionCodes.IllegalFunction => new ModbusFault(FaultType.IllegalFunction, "ModbusExceptionCode01"),
            _ => new ModbusFault(FaultType.Unknown, $"ModbusExceptionCode{(byte)exception.SlaveExceptionCode:D2}")
        };

    private static ModbusFault FromSocketException(SocketException exception) =>
        exception.SocketErrorCode switch
        {
            SocketError.ConnectionRefused => new ModbusFault(FaultType.ConnectionRefused, nameof(SocketError.ConnectionRefused)),
            SocketError.ConnectionReset => new ModbusFault(FaultType.ConnectionReset, nameof(SocketError.ConnectionReset)),
            SocketError.TimedOut => new ModbusFault(FaultType.Timeout, nameof(SocketError.TimedOut)),
            SocketError.HostNotFound or SocketError.HostUnreachable or SocketError.NetworkUnreachable =>
                new ModbusFault(FaultType.ConnectionLost, exception.SocketErrorCode.ToString()),
            _ => new ModbusFault(FaultType.Unknown, exception.SocketErrorCode.ToString())
        };
}
