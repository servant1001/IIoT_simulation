using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Protocols.OpcUa;

public static class OpcUaFaultNormalizer
{
    public static FaultType Normalize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var text = exception.ToString();
        if (text.Contains("BadNodeIdUnknown", StringComparison.OrdinalIgnoreCase))
        {
            return FaultType.InvalidNodeId;
        }

        if (text.Contains("BadSession", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("SecureChannel", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("BadConnectionClosed", StringComparison.OrdinalIgnoreCase) ||
            (text.Contains("Session", StringComparison.OrdinalIgnoreCase) && text.Contains("Closed", StringComparison.OrdinalIgnoreCase)))
        {
            return FaultType.SessionDisconnected;
        }

        if (exception.GetBaseException() is System.Net.Sockets.SocketException ||
            text.Contains("Endpoint", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Connection", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("refused", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("拒絕連線", StringComparison.Ordinal))
        {
            return FaultType.InvalidEndpoint;
        }

        if (exception.GetBaseException() is TimeoutException || text.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return FaultType.Timeout;
        }

        if (text.Contains("Certificate", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("BadSecurityChecksFailed", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("BadCertificate", StringComparison.OrdinalIgnoreCase))
        {
            return FaultType.CertificateError;
        }

        return FaultType.Unknown;
    }
}
