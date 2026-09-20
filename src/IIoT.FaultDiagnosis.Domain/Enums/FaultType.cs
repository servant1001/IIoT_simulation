namespace IIoT.FaultDiagnosis.Domain.Enums;

public enum FaultType
{
    None = 0,
    ConnectionRefused,
    ConnectionReset,
    ConnectionLost,
    Timeout,
    WrongPort,
    AuthenticationFailed,
    WrongSlaveId,
    IllegalAddress,
    IllegalFunction,
    InvalidResponseLength,
    InvalidDataType,
    InvalidEndpoint,
    InvalidNodeId,
    CertificateError,
    SessionDisconnected,
    BrokerUnavailable,
    TopicMismatch,
    PayloadInvalid,
    NetworkDelay,
    PacketLoss,
    ServerRestart,
    Unknown
}
