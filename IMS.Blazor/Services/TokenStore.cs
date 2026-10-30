using Microsoft.JSInterop;

namespace IMS.Blazor.Services;

/// <summary>
/// Keeps the JWT in memory (fast, used by every API call) and in the browser's localStorage
/// (so a page refresh does not log the user out).
/// </summary>
public class TokenStore
{
    private const string TokenKey = "ims.token";
    private const string ExpirationKey = "ims.expiration";

    private readonly IJSRuntime _js;

    public TokenStore(IJSRuntime js) => _js = js;

    public string? Token { get; private set; }

    public string? Expiration { get; private set; }

    /// <summary>Raised after login / logout so the screens can react.</summary>
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        Token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        Expiration = await _js.InvokeAsync<string?>("localStorage.getItem", ExpirationKey);
    }

    public async Task SetAsync(string token, string? expiration)
    {
        Token = token;
        Expiration = expiration;

        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await _js.InvokeVoidAsync("localStorage.setItem", ExpirationKey, expiration ?? "");

        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        Token = null;
        Expiration = null;

        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", ExpirationKey);

        Changed?.Invoke();
    }
}
