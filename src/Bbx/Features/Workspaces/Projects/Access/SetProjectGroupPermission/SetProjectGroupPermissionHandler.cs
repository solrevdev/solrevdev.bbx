using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.Access.SetProjectGroupPermission;

public sealed class SetProjectGroupPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    // "none" is not a permission Bitbucket accepts here; it answers 400
    // "none is not a valid permission". Use the remove verb instead.
    public static readonly string[] Allowed = ["read", "write", "create-repo", "admin"];

    public async Task<object> HandleAsync(SetProjectGroupPermissionRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);
        PermissionGrant.EnsureValid(request.Permission, Allowed);

        var grant = await client.PutAsync<JsonElement>(
            $"workspaces/{ws}/projects/{key}/permissions-config/groups/{Uri.EscapeDataString(request.GroupSlug)}",
            new { permission = request.Permission.ToLowerInvariant() }, ct);
        return PermissionGrant.Project(grant);
    }
}
