using System.Net;
using System.Net.Http.Headers;

namespace IMS.Blazor.Services;

/// <summary>
/// Runs on every API call: adds the Bearer token, and if the API answers 401 (token expired)
/// it clears the login so the layout sends the user back to the login page.
/// </summary>
public class JwtHandler : DelegatingHandler
{
    private readonly TokenStore _store;

    public JwtHandler(TokenStore store) => _store = store;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var isLogin = path.EndsWith("/login", StringComparison.OrdinalIgnoreCase);

        if (!isLogin && !string.IsNullOrEmpty(_store.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _store.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (!isLogin && response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_store.Token))
        {
            await _store.ClearAsync();
        }

        return response;
    }
}
