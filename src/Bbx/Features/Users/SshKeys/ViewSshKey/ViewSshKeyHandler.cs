using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users;
using Bbx.Features.Users.SshKeys.ListSshKeys;

namespace Bbx.Features.Users.SshKeys.ViewSshKey;

public sealed class ViewSshKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewSshKeyRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");
        if (string.IsNullOrEmpty(request.KeyId))
            throw new BbxUserException("Error: <key-id> is required.");

        var user = await UserSelector.ResolveAsync(client, request.SelectedUser, ct);
        var key = await client.GetAsync<JsonElement>(
            $"/users/{Uri.EscapeDataString(user)}/ssh-keys/{Uri.EscapeDataString(request.KeyId)}", ct);
        return ListSshKeysHandler.ProjectKey(key);
    }
}
