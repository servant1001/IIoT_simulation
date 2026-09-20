using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Protocols.Mqtt;

public static class MqttFaultNormalizer
{
    public static FaultType Normalize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is System.Text.Json.JsonException)
            return FaultType.PayloadInvalid;

        var text = exception.ToString();
        if (text.Contains("NotAuthorized", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("BadUserNameOrPassword", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
            return FaultType.AuthenticationFailed;
        if (text.Contains("Topic", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Subscription", StringComparison.OrdinalIgnoreCase))
            return FaultType.TopicMismatch;
        if (text.Contains("Payload", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Json", StringComparison.OrdinalIgnoreCase))
            return FaultType.PayloadInvalid;
        if (text.Contains("Disconnected", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ConnectionLost", StringComparison.OrdinalIgnoreCase))
            return FaultType.ConnectionLost;
        if (exception.GetBaseException() is System.Net.Sockets.SocketException ||
            text.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Broker", StringComparison.OrdinalIgnoreCase))
            return FaultType.BrokerUnavailable;

        return FaultType.Unknown;
    }
}
