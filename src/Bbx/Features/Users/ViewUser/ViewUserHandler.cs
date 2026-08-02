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
        // No selector means "show me". Bitbucket exposes that as /2.0/user,
        // a different route from /2.0/users/{selected_user}.
        if (string.IsNullOrEmpty(request.SelectedUser)
            || request.SelectedUser.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            return await client.GetAsync<JsonElement>("user", ct);
        }

        return await client.GetAsync<JsonElement>(
            $"/users/{Uri.EscapeDataString(request.SelectedUser)}", ct);
    }
}
