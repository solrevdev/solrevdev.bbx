using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users.SshKeys.ListSshKeys;

namespace Bbx.Features.Users.SshKeys.AddSshKey;

public sealed class AddSshKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddSshKeyRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");
        if (string.IsNullOrWhiteSpace(request.Key))
            throw new BbxUserException("Error: --key (the public SSH key body) is required.");

        var user = string.IsNullOrEmpty(request.SelectedUser) ? "me" : request.SelectedUser;
        var body = new Dictionary<string, object> { ["key"] = request.Key };
        if (!string.IsNullOrEmpty(request.Label)) body["label"] = request.Label;

        var key = await client.PostAsync<JsonElement>(
            $"/users/{Uri.EscapeDataString(user)}/ssh-keys", body, ct);
        return ListSshKeysHandler.ProjectKey(key);
    }
}
