using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.Access.RemoveProjectGroupPermission;

public sealed class RemoveProjectGroupPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RemoveProjectGroupPermissionRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);
        await client.DeleteAsync(
            $"workspaces/{ws}/projects/{key}/permissions-config/groups/{Uri.EscapeDataString(request.GroupSlug)}", ct);
        return $"\u2713 Removed the explicit permission for {request.GroupSlug} on project {request.ProjectKey}";
    }
}
