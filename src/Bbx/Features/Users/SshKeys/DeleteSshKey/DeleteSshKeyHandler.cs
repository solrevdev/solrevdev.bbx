using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users;

namespace Bbx.Features.Users.SshKeys.DeleteSshKey;

public sealed class DeleteSshKeyHandler(BitbucketClient client)
{
    public async Task<string> HandleAsync(DeleteSshKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.KeyId))
            throw new BbxUserException("Error: <key-id> is required.");

        var user = await UserSelector.ResolveAsync(client, request.SelectedUser, ct);
        await client.DeleteAsync(
            $"/users/{Uri.EscapeDataString(user)}/ssh-keys/{Uri.EscapeDataString(request.KeyId)}", ct);
        return $"✓ Deleted SSH key '{request.KeyId}' for {user}";
    }
}
