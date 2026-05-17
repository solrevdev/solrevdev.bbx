using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Users.SshKeys.ListSshKeys;

public sealed class ListSshKeysHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListSshKeysRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var user = string.IsNullOrEmpty(request.SelectedUser) ? "me" : request.SelectedUser;
        var keys = new List<object>();
        var count = 0;
        await foreach (var k in client.GetPaginatedAsync<JsonElement>(
            $"/users/{Uri.EscapeDataString(user)}/ssh-keys", ct))
        {
            keys.Add(ProjectKey(k));
            if (++count >= request.Limit) break;
        }

        return new { user, count = keys.Count, ssh_keys = keys };
    }

    internal static object ProjectKey(JsonElement k) => new
    {
        uuid = k.TryGetProperty("uuid", out var u) ? u.GetString() : null,
        label = k.TryGetProperty("label", out var l) ? l.GetString() : null,
        key = k.TryGetProperty("key", out var key) ? key.GetString() : null,
        comment = k.TryGetProperty("comment", out var c) ? c.GetString() : null,
        type = k.TryGetProperty("type", out var t) ? t.GetString() : null,
        created_on = k.TryGetProperty("created_on", out var co) ? co.GetString() : null,
        last_used = k.TryGetProperty("last_used", out var lu) ? lu.GetString() : null,
    };
}
