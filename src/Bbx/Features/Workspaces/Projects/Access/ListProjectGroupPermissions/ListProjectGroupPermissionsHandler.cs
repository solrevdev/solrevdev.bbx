using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.Access.ListProjectGroupPermissions;

public sealed class ListProjectGroupPermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListProjectGroupPermissionsRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);

        var permissions = new List<object>();
        var count = 0;
        await foreach (var grant in client.GetPaginatedAsync<JsonElement>(
            $"workspaces/{ws}/projects/{key}/permissions-config/groups", ct))
        {
            permissions.Add(PermissionGrant.Project(grant));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, project_key = request.ProjectKey, count = permissions.Count, permissions };
    }
}
