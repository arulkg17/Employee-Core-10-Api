using IMS.Blazor.Models;

namespace IMS.Blazor.Services;

public class AuthService
{
    private readonly ApiClient _api;
    private readonly TokenStore _store;

    public AuthService(ApiClient api, TokenStore store)
    {
        _api = api;
        _store = store;
    }

    /// <summary>POST api/v1/Login. On success the token is stored and the user counts as logged in.</summary>
    public async Task<ApiResult> LoginAsync(string userName, string password)
    {
        var result = await _api.PostAsync("v1/Login", new LoginRequest { UserName = userName, Password = password });

        if (!result.Success)
        {
            return result;
        }

        var login = result.As<LoginResponse>();

        if (login is null || string.IsNullOrWhiteSpace(login.Token))
        {
            return ApiResult.Fail("Login failed: the API did not return a token.");
        }

        await _store.SetAsync(login.Token, login.Expiration);

        return result;
    }

    public Task LogoutAsync() => _store.ClearAsync();
}
