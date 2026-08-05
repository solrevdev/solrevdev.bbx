using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.SetRepoGroupPermission;

public sealed class SetRepoGroupPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public static readonly string[] Allowed = ["read", "write", "admin", "none"];

    public async Task<object> HandleAsync(SetRepoGroupPermissionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        PermissionGrant.EnsureValid(request.Permission, Allowed);

        var grant = await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/permissions-config/groups/{Uri.EscapeDataString(request.GroupSlug)}",
            new { permission = request.Permission.ToLowerInvariant() }, ct);
        return PermissionGrant.Project(grant);
    }
}
