using System.Text.Json;
using Bbx.Api;
using Bbx.Features.Users.GpgKeys.ListGpgKeys;

namespace Bbx.Features.Users.GpgKeys.ViewGpgKey;

public sealed class ViewGpgKeyHandler(BitbucketClient client)
{
    public async Task<object> HandleAsync(ViewGpgKeyRequest request, CancellationToken ct)
    {
        var user = await UserSelector.ResolveAsync(client, request.SelectedUser, ct);
        var key = await client.GetAsync<JsonElement>(
            $"users/{Uri.EscapeDataString(user)}/gpg-keys/{Uri.EscapeDataString(request.Fingerprint)}", ct);
        return ListGpgKeysHandler.ProjectKey(key);
    }
}
