using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users;
using Bbx.Features.Users.SshKeys.ListSshKeys;

namespace Bbx.Features.Users.SshKeys.AddSshKey;

public sealed class AddSshKeyHandler(BitbucketClient client)
{
    public async Task<object> HandleAsync(AddSshKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            throw new BbxUserException("Error: --key (the public SSH key body) is required.");

        var user = await UserSelector.ResolveAsync(client, request.SelectedUser, ct);
        var body = new Dictionary<string, object> { ["key"] = request.Key };
        if (!string.IsNullOrEmpty(request.Label)) body["label"] = request.Label;

        var key = await client.PostAsync<JsonElement>(
            $"/users/{Uri.EscapeDataString(user)}/ssh-keys", body, ct);
        return ListSshKeysHandler.ProjectKey(key);
    }
}
