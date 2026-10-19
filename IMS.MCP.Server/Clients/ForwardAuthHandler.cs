namespace IMS.MCP.Server.Clients;

/// <summary>
/// Passes the MCP client's Authorization header on to CoreAPI and Invoice.RAG, so the same JWT decides what the
/// caller may see. If CategoryTools already has its own token handling (for example a login call), plug it in here:
/// this is the only place the two new HTTP clients get their token.
/// </summary>
public sealed class ForwardAuthHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
            request.Headers.TryAddWithoutValidation("Authorization", header);

        return base.SendAsync(request, cancellationToken);
    }
}
