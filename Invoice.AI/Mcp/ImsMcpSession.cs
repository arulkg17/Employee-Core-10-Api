using Azure.Core.Pipeline;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Invoice.AI.Mcp;

/// <summary>
/// A connection from Invoice.AI to IMS.MCP.Server for ONE request. The caller's Authorization header is passed on,
/// so the MCP server (and CoreAPI / Invoice.RAG behind it) see the same user. Invoice.AI then needs no database access
/// and no copy of the vendor tools: it uses exactly the tools Copilot and MCP Inspector use.
/// Built for ModelContextProtocol 1.x (McpClient + HttpClientTransport). Older 0.x versions call these
/// McpClientFactory + SseClientTransport.
/// </summary>
public sealed class ImsMcpSession : IImsMcpSession
{
    private readonly McpClient _client;

    private ImsMcpSession(McpClient client) => _client = client;

    public static async Task<IImsMcpSession> ConnectAsync(string endpoint, string? authorization, CancellationToken ct)
    {
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(authorization))
            headers["Authorization"] = authorization;

        var transport = new ModelContextProtocol.Client.HttpClientTransport(new ModelContextProtocol.Client.HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            Name = "IMS.MCP.Server",
            AdditionalHeaders = headers
        });

        return new ImsMcpSession(await McpClient.CreateAsync(transport, cancellationToken: ct));
    }

    public async Task<IList<McpClientTool>> ListToolsAsync(CancellationToken ct)
        => await _client.ListToolsAsync(cancellationToken: ct);

    /// <summary>Calls one MCP tool and returns its text result (the skill tools return markdown or JSON text).</summary>
    public async Task<string> CallToolTextAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken ct)
    {
        var result = await _client.CallToolAsync(name, arguments, cancellationToken: ct);
        return string.Join("\n", result.Content.OfType<TextContentBlock>().Select(b => b.Text));
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
