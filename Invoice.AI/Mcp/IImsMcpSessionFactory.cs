namespace Invoice.AI.Mcp;

public interface IImsMcpSessionFactory
{
    Task<IImsMcpSession> CreateAsync(string endpoint, string? authorization, CancellationToken ct);
}

public sealed class DefaultImsMcpSessionFactory : IImsMcpSessionFactory
{
    public Task<IImsMcpSession> CreateAsync(string endpoint, string? authorization, CancellationToken ct)
        => ImsMcpSession.ConnectAsync(endpoint, authorization, ct);
}
