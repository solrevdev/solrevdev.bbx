using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.RepoPermissions;

public sealed class RepoPermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(RepoPermissionsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.RepoPath(credentials, request.Workspace, request.Repository,
            "Error: Invalid repository path.");

        var permissions = new List<object>();
        await foreach (var perm in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/permissions-config/users", ct))
        {
            permissions.Add(new
            {
                type = "user",
                user = perm.TryGetProperty("user", out var u) ? u.GetProperty("display_name").GetString() : null,
                permission = perm.TryGetProperty("permission", out var p) ? p.GetString() : null,
            });
        }

        return new { count = permissions.Count, permissions };
    }
}
