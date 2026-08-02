using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users;

namespace Bbx.Features.Users.SshKeys.DeleteSshKey;

public sealed class DeleteSshKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteSshKeyRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");
        if (string.IsNullOrEmpty(request.KeyId))
            throw new BbxUserException("Error: <key-id> is required.");

        var user = await UserSelector.ResolveAsync(client, request.SelectedUser, ct);
        await client.DeleteAsync(
            $"/users/{Uri.EscapeDataString(user)}/ssh-keys/{Uri.EscapeDataString(request.KeyId)}", ct);
        return $"✓ Deleted SSH key '{request.KeyId}' for {user}";
    }
}
