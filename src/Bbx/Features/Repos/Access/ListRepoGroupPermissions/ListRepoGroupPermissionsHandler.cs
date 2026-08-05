using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.ListRepoGroupPermissions;

public sealed class ListRepoGroupPermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListRepoGroupPermissionsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var permissions = new List<object>();
        var count = 0;
        await foreach (var grant in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repo}/permissions-config/groups", ct))
        {
            permissions.Add(PermissionGrant.Project(grant));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = permissions.Count, permissions };
    }
}
