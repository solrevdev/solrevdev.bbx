using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Users.ListUserWorkspacePermissions;

public sealed class ListUserWorkspacePermissionsHandler(BitbucketClient client)
{
    public async Task<object> HandleAsync(ListUserWorkspacePermissionsRequest request, CancellationToken ct)
    {

        var perms = new List<object>();
        var count = 0;
        await foreach (var p in client.GetPaginatedAsync<JsonElement>("/user/permissions/workspaces", ct))
        {
            perms.Add(new
            {
                permission = p.TryGetProperty("permission", out var perm) ? perm.GetString() : null,
                last_accessed = p.TryGetProperty("last_accessed", out var la) ? la.GetString() : null,
                added_on = p.TryGetProperty("added_on", out var ao) ? ao.GetString() : null,
                workspace_slug = p.TryGetObject("workspace", out var w) && w.TryGetProperty("slug", out var ws)
                    ? ws.GetString()
                    : null,
                workspace_name = p.TryGetObject("workspace", out var w2) && w2.TryGetProperty("name", out var wn)
                    ? wn.GetString()
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { count = perms.Count, workspace_permissions = perms };
    }
}
