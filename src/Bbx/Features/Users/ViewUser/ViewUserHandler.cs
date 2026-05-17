using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Users.ViewUser;

public sealed class ViewUserHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewUserRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");
        if (string.IsNullOrEmpty(request.SelectedUser))
            throw new BbxUserException("Error: <selected-user> is required (UUID, account ID, or username).");

        return await client.GetAsync<JsonElement>(
            $"/users/{Uri.EscapeDataString(request.SelectedUser)}", ct);
    }
}
