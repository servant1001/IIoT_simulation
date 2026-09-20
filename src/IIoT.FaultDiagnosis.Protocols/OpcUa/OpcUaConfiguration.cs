namespace IIoT.FaultDiagnosis.Protocols.OpcUa;

public sealed record OpcUaConfiguration(
    string Endpoint,
    IReadOnlyList<string> NodeIds,
    int ReconnectDelayMs = 1000,
    bool UseSecurity = false,
    int OperationTimeoutMs = 10000)
{
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, "opc.tcp", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Endpoint must use the opc.tcp scheme.", nameof(Endpoint));
        if (NodeIds is null || NodeIds.Count == 0 || NodeIds.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one OPC UA NodeId is required.", nameof(NodeIds));
        if (ReconnectDelayMs < 100) throw new ArgumentOutOfRangeException(nameof(ReconnectDelayMs));
        if (OperationTimeoutMs < 100) throw new ArgumentOutOfRangeException(nameof(OperationTimeoutMs));
    }
}
