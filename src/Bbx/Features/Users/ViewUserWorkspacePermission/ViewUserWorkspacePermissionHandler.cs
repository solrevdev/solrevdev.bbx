using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Users.ViewUserWorkspacePermission;

public sealed class ViewUserWorkspacePermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewUserWorkspacePermissionRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        return await client.GetAsync<JsonElement>(
            $"user/workspaces/{Uri.EscapeDataString(workspace)}/permission", ct);
    }
}
