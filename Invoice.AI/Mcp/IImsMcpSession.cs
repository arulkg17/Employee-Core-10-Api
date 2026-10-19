using ModelContextProtocol.Client;
using System.Threading.Tasks;

namespace Invoice.AI.Mcp;

public interface IImsMcpSession : IAsyncDisposable
{
    Task<IList<McpClientTool>> ListToolsAsync(CancellationToken ct);
    Task<string> CallToolTextAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken ct);
}
