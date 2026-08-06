using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.Access.ViewProjectUserPermission;

public sealed class ViewProjectUserPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewProjectUserPermissionRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);
        var grant = await client.GetAsync<JsonElement>(
            $"workspaces/{ws}/projects/{key}/permissions-config/users/{Uri.EscapeDataString(request.AccountId)}", ct);
        return PermissionGrant.Project(grant);
    }
}
