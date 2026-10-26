using IIoT.FaultDiagnosis.Domain.Enums;

namespace IIoT.FaultDiagnosis.Application.Rag;

public interface IRagKnowledgeBase
{
    IReadOnlyList<(RagKnowledgeArticle Article, int Score)> Search(RagRecordContext context, int maximumCount);
}

public sealed class RagKnowledgeBase : IRagKnowledgeBase
{
    private static readonly IReadOnlyList<RagKnowledgeArticle> Articles =
    [
        Article("modbus-connection-refused", "Modbus TCP connection refused", ProtocolType.ModbusTcp, FaultType.ConnectionRefused,
            ["connection refused", "actively refused", "econnrefused"],
            ["The Modbus simulator or PLC is not listening on the configured port.", "A firewall or incorrect endpoint can reject the TCP connection."],
            ["Confirm the configured host and port.", "Start the Modbus simulator and verify that it listens on the expected TCP port."]),
        Article("modbus-timeout", "Modbus TCP response timeout", ProtocolType.ModbusTcp, FaultType.Timeout,
            ["timeout", "timed out", "read timeout"],
            ["The target accepted a connection but did not return a response before the configured timeout.", "Network delay or device load can delay a Modbus response."],
            ["Verify simulator responsiveness and network latency.", "Increase the operation timeout only after confirming the register request is valid."]),
        Article("modbus-illegal-address", "Modbus illegal register address", ProtocolType.ModbusTcp, FaultType.IllegalAddress,
            ["illegal address", "exception code 2", "illegal data address"],
            ["The requested register address is outside the device register map.", "The selected register type or start address does not match the simulator configuration."],
            ["Confirm the slave ID, register type and register map.", "Test the address with a known Modbus client before changing retry settings."]),
        Article("opcua-invalid-endpoint", "OPC UA invalid endpoint", ProtocolType.OpcUa, FaultType.InvalidEndpoint,
            ["endpoint", "bad tcp endpoint", "endpoint url"],
            ["The endpoint URL or port does not match a published OPC UA endpoint.", "The OPC UA simulation server may not be running."],
            ["Copy the endpoint URL from the OPC UA server configuration.", "Confirm the server process and configured port are available."]),
        Article("opcua-invalid-node", "OPC UA invalid node", ProtocolType.OpcUa, FaultType.InvalidNodeId,
            ["node id", "badnodeidunknown", "nodeid"],
            ["The requested NodeId is not exposed by the server namespace.", "The NodeId syntax or namespace index may be incorrect."],
            ["Browse the server address space and copy the NodeId.", "Verify namespace indexes after a simulator restart."]),
        Article("opcua-certificate", "OPC UA certificate validation", ProtocolType.OpcUa, FaultType.CertificateError,
            ["certificate", "untrusted", "security"],
            ["The server rejected the client certificate or the client did not trust the server certificate.", "The selected security endpoint requires certificate configuration."],
            ["Trust the server certificate in the client store.", "Register the client certificate in the server trust list before using a secure endpoint."]),
        Article("mqtt-broker-unavailable", "MQTT broker unavailable", ProtocolType.Mqtt, FaultType.BrokerUnavailable,
            ["broker unavailable", "server unavailable", "connection refused"],
            ["The MQTT broker is stopped, unreachable or listening on another port.", "The client host or port configuration does not target the broker."],
            ["Confirm Mosquitto or the external broker is running.", "Verify the broker host, port and Docker port mapping."]),
        Article("mqtt-authentication", "MQTT authentication failed", ProtocolType.Mqtt, FaultType.AuthenticationFailed,
            ["not authorized", "authentication", "bad user name", "bad password"],
            ["The broker rejected the configured credentials.", "Anonymous connections may be disabled on the broker."],
            ["Verify username and password with the broker administrator.", "Confirm the broker ACL allows the client to connect and subscribe."]),
        Article("mqtt-topic", "MQTT topic mismatch", ProtocolType.Mqtt, FaultType.TopicMismatch,
            ["topic", "subscription", "no matching"],
            ["The publisher and subscriber use different topic names or wildcard scopes.", "The broker ACL may allow the connection but deny the requested topic."],
            ["Compare the publisher topic with the device subscription exactly.", "Check topic ACL rules and retained-message settings."]),
        Article("mqtt-payload", "MQTT payload format invalid", ProtocolType.Mqtt, FaultType.PayloadInvalid,
            ["payload", "json", "deserialize", "parse"],
            ["The received payload does not meet the configured JSON or schema expectation.", "The publisher may use a different encoding or message structure."],
            ["Capture the raw payload and validate it against the expected schema.", "Align publisher encoding and required JSON fields."])
    ];

    public IReadOnlyList<(RagKnowledgeArticle Article, int Score)> Search(RagRecordContext context, int maximumCount)
    {
        var query = string.Join(' ', context.ObservedFaultType, context.ErrorCode, context.ErrorMessage).ToLowerInvariant();
        return Articles
            .Select(article => (Article: article, Score: Score(article, context, query)))
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Article.Id, StringComparer.Ordinal)
            .Take(maximumCount)
            .ToArray();
    }

    private static int Score(RagKnowledgeArticle article, RagRecordContext context, string query)
    {
        var score = article.ProtocolType == context.ProtocolType ? 6 : 0;
        if (article.FaultType == context.ObservedFaultType)
        {
            score += 12;
        }

        score += article.Keywords.Count(keyword => query.Contains(keyword, StringComparison.OrdinalIgnoreCase)) * 3;
        return score;
    }

    private static RagKnowledgeArticle Article(string id, string title, ProtocolType protocolType, FaultType faultType, string[] keywords, string[] possibleCauses, string[] suggestedActions) =>
        new(id, title, protocolType, faultType, keywords, possibleCauses, suggestedActions);
}
