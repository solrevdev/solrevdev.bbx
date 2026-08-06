using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.SetRepoUserPermission;

public sealed class SetRepoUserPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    // "none" is not a permission Bitbucket accepts here; it answers 400
    // "none is not a valid permission". Use the remove verb instead.
    public static readonly string[] Allowed = ["read", "write", "admin"];

    public async Task<object> HandleAsync(SetRepoUserPermissionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        PermissionGrant.EnsureValid(request.Permission, Allowed);

        var grant = await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/permissions-config/users/{Uri.EscapeDataString(request.AccountId)}",
            new { permission = request.Permission.ToLowerInvariant() }, ct);
        return PermissionGrant.Project(grant);
    }
}
