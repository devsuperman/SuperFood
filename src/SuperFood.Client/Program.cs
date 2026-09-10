using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Authorization;
using MudBlazor.Services;
using SuperFood.Client;
using SuperFood.Client.Auth;
using SuperFood.Client.Modules.Catalog;
using SuperFood.Contracts.Users;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<ILocalStorageService, BrowserLocalStorageService>();
builder.Services.AddScoped<AuthSessionStore>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
// Mirrors the server's policies (SuperFood.Api/Program.cs) - client-side
// [Authorize(Policy=...)] is a separate, UI-only check; the API still
// re-validates every permission on each request.
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("platform_admin", policy => policy.RequireClaim("is_platform_admin", "true"));
    foreach (var permission in AvailablePermissions.All)
    {
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
    }
});
builder.Services.AddScoped<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<ITokenAccessor, TokenAccessor>();
builder.Services.AddScoped(_ => new Uri(apiBaseUrl));

builder.Services.AddMudServices();
builder.Services.AddScoped<LazyAssemblyLoader>();
builder.Services.AddScoped<CartState>();

await builder.Build().RunAsync();
