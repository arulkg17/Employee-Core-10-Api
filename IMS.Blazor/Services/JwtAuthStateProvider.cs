using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace IMS.Blazor.Services;

/// <summary>
/// Tells Blazor who the user is, based on the JWT in the TokenStore.
/// &lt;AuthorizeView&gt; and the layout use this to show / hide things.
/// </summary>
public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStore _store;

    public JwtAuthStateProvider(TokenStore store)
    {
        _store = store;

        // login / logout / 401 -> tell Blazor to ask again
        _store.Changed += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(Build());

    private AuthenticationState Build()
    {
        var token = _store.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        var claims = JwtParser.ParseClaims(token);

        if (claims is null)
        {
            return Anonymous;
        }

        // the token's own "exp" first, then the expiration the login call returned
        var expiry = JwtParser.GetExpiry(claims);

        if (expiry is null && DateTimeOffset.TryParse(_store.Expiration, out var stored))
        {
            expiry = stored;
        }

        if (expiry is not null && expiry <= DateTimeOffset.UtcNow)
        {
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
