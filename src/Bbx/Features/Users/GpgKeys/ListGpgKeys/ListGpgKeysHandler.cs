using System.Text.Json;
using Bbx.Api;

namespace Bbx.Features.Users.GpgKeys.ListGpgKeys;

public sealed class ListGpgKeysHandler(BitbucketClient client)
{
    public async Task<object> HandleAsync(ListGpgKeysRequest request, CancellationToken ct)
    {
        var user = await UserSelector.ResolveAsync(client, request.SelectedUser, ct);

        var keys = new List<object>();
        var count = 0;
        await foreach (var key in client.GetPaginatedAsync<JsonElement>(
            $"users/{Uri.EscapeDataString(user)}/gpg-keys", ct))
        {
            keys.Add(ProjectKey(key));
            if (++count >= request.Limit) break;
        }

        return new { user, count = keys.Count, gpg_keys = keys };
    }

    internal static object ProjectKey(JsonElement key) => new
    {
        fingerprint = key.GetStringOrNull("fingerprint"),
        key_id = key.GetStringOrNull("key_id"),
        name = key.GetStringOrNull("name"),
        created_on = key.GetStringOrNull("created_on"),
        expires_on = key.GetStringOrNull("expires_on"),
    };
}
