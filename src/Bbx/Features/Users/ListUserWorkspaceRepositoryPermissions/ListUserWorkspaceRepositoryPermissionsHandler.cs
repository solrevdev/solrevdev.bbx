using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Users.ListUserWorkspaceRepositoryPermissions;

public sealed class ListUserWorkspaceRepositoryPermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListUserWorkspaceRepositoryPermissionsRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        // Narrowed to one workspace. The unscoped /user/permissions/repositories
        // that `user permissions repositories` reads spans every workspace.
        var perms = new List<object>();
        var count = 0;
        await foreach (var p in client.GetPaginatedAsync<JsonElement>(
            $"user/workspaces/{Uri.EscapeDataString(workspace)}/permissions/repositories", ct))
        {
            perms.Add(new
            {
                permission = p.GetStringOrNull("permission"),
                repository_full_name = p.TryGetObject("repository", out var r) ? r.GetStringOrNull("full_name") : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace, count = perms.Count, repository_permissions = perms };
    }
}
