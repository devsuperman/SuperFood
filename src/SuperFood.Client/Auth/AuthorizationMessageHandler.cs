using System.Net.Http.Headers;

namespace SuperFood.Client.Auth;

/// <summary>Attaches the stored bearer token to every API call (docs/tech-stack.md §9.4).</summary>
public class AuthorizationMessageHandler(AuthSessionStore sessionStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetAsync();
        if (session is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
