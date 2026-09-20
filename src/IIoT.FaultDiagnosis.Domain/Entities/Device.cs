using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Domain.Entities;

public sealed class Device
{
    private Device()
    {
    }

    public Device(Guid id, string name, ProtocolType protocolType, string host, int port, bool isEnabled, DateTimeOffset now)
    {
        Id = id;
        Update(name, protocolType, host, port, isEnabled, now);
        CreatedAt = now;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ProtocolType ProtocolType { get; private set; }
    public string Host { get; private set; } = string.Empty;
    public int Port { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string name, ProtocolType protocolType, string host, int port, bool isEnabled, DateTimeOffset now)
    {
        Name = RequireValue(name, nameof(name), 200);
        Host = RequireValue(host, nameof(host), 255);
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        ProtocolType = protocolType;
        Port = port;
        IsEnabled = isEnabled;
        UpdatedAt = now;
    }

    private static string RequireValue(string value, string parameterName, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
        {
            throw new ArgumentException($"{parameterName} must contain between 1 and {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}
