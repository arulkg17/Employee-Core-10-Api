namespace Invoice.RAG.Services;

/// <summary>Passes the caller's JWT on to CoreAPI so CoreAPI still decides what that user may see.</summary>
public sealed class ForwardJwtHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
            request.Headers.TryAddWithoutValidation("Authorization", header);

        return base.SendAsync(request, cancellationToken);
    }
}
