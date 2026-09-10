using System.Net.Http.Json;
using SuperFood.Contracts.Auth;

namespace SuperFood.Client.Auth;

public class AuthApiClient(HttpClient http, AuthSessionStore sessionStore, ApiAuthenticationStateProvider authStateProvider)
{
    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (login is null)
        {
            return false;
        }

        await StoreSessionAsync(login);
        return true;
    }

    public async Task LogoutAsync()
    {
        await sessionStore.ClearAsync();
        authStateProvider.NotifySignedOut();
    }

    private async Task StoreSessionAsync(LoginResponse login)
    {
        var session = new AuthSession(
            login.AccessToken, login.AccessTokenExpiresAt, login.RefreshToken, login.UserId, login.FullName,
            login.RestaurantId, login.IsPlatformAdmin, login.Permissions);

        await sessionStore.SetAsync(session);
        authStateProvider.NotifySignedIn(session);
    }
}
