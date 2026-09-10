using Microsoft.JSInterop;

namespace SuperFood.Client.Auth;

/// <summary>
/// Wraps the browser's own localStorage via JS interop directly - no interop
/// JS file needed since these are calls straight into a built-in API.
/// </summary>
public class BrowserLocalStorageService(IJSRuntime jsRuntime) : ILocalStorageService
{
    public async Task<string?> GetItemAsync(string key) =>
        await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task SetItemAsync(string key, string value) =>
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

    public async Task RemoveItemAsync(string key) =>
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}
