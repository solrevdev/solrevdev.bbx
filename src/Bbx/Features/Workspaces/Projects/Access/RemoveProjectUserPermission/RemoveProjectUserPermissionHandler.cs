using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.Access.RemoveProjectUserPermission;

public sealed class RemoveProjectUserPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RemoveProjectUserPermissionRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);
        await client.DeleteAsync(
            $"workspaces/{ws}/projects/{key}/permissions-config/users/{Uri.EscapeDataString(request.AccountId)}", ct);
        return $"\u2713 Removed the explicit permission for {request.AccountId} on project {request.ProjectKey}";
    }
}
