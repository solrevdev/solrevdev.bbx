using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.Access.SetProjectUserPermission;

public sealed class SetProjectUserPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public static readonly string[] Allowed = ["read", "write", "create-repo", "admin", "none"];

    public async Task<object> HandleAsync(SetProjectUserPermissionRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);
        PermissionGrant.EnsureValid(request.Permission, Allowed);

        var grant = await client.PutAsync<JsonElement>(
            $"workspaces/{ws}/projects/{key}/permissions-config/users/{Uri.EscapeDataString(request.AccountId)}",
            new { permission = request.Permission.ToLowerInvariant() }, ct);
        return PermissionGrant.Project(grant);
    }
}
