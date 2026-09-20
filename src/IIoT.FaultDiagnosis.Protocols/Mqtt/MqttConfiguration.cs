namespace IIoT.FaultDiagnosis.Protocols.Mqtt;

public sealed record MqttConfiguration(
    string Topic,
    int QualityOfService = 1,
    int ReceiveTimeoutMs = 10000,
    int ReconnectDelayMs = 1000,
    bool RequireJsonPayload = true,
    string? Username = null,
    string? Password = null)
{
    public static MqttConfiguration Default { get; } = new("factory/machine01/data");

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Topic) || Topic.Contains('#') || Topic.Contains('+'))
            throw new ArgumentException("Topic must be a non-empty concrete MQTT topic.", nameof(Topic));
        if (QualityOfService is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(QualityOfService));
        if (ReceiveTimeoutMs < 100)
            throw new ArgumentOutOfRangeException(nameof(ReceiveTimeoutMs));
        if (ReconnectDelayMs < 100)
            throw new ArgumentOutOfRangeException(nameof(ReconnectDelayMs));
        if (Password is not null && string.IsNullOrWhiteSpace(Username))
            throw new ArgumentException("Username is required when a password is provided.", nameof(Username));
    }
}
