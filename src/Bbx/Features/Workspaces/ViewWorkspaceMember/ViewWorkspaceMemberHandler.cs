using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ViewWorkspaceMember;

public sealed class ViewWorkspaceMemberHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewWorkspaceMemberRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        // The selector is an account UUID or account ID. Usernames stopped
        // working when Bitbucket withdrew them.
        return await client.GetAsync<JsonElement>(
            $"workspaces/{workspace}/members/{Uri.EscapeDataString(request.Member)}", ct);
    }
}
