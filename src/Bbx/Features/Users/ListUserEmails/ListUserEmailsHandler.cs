using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Users.ListUserEmails;

public sealed class ListUserEmailsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListUserEmailsRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var emails = new List<object>();
        var count = 0;
        await foreach (var e in client.GetPaginatedAsync<JsonElement>("/user/emails", ct))
        {
            emails.Add(new
            {
                email = e.TryGetProperty("email", out var em) ? em.GetString() : null,
                is_primary = e.TryGetProperty("is_primary", out var p) && p.GetBoolean(),
                is_confirmed = e.TryGetProperty("is_confirmed", out var c) && c.GetBoolean(),
                type = e.TryGetProperty("type", out var t) ? t.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { count = emails.Count, emails };
    }
}
