using System.Text.Json;

namespace SuperFood.Client.Auth;

/// <summary>Reads/writes the current <see cref="AuthSession"/> to local storage.</summary>
public class AuthSessionStore(ILocalStorageService localStorage)
{
    private const string StorageKey = "superfood.session";
    private AuthSession? _cached;

    public async Task<AuthSession?> GetAsync()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var json = await localStorage.GetItemAsync(StorageKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        _cached = JsonSerializer.Deserialize<AuthSession>(json);
        return _cached;
    }

    public async Task SetAsync(AuthSession session)
    {
        _cached = session;
        await localStorage.SetItemAsync(StorageKey, JsonSerializer.Serialize(session));
    }

    public async Task ClearAsync()
    {
        _cached = null;
        await localStorage.RemoveItemAsync(StorageKey);
    }
}
