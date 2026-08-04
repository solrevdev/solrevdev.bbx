using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ListWorkspacePermissions;

public sealed class ListWorkspacePermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWorkspacePermissionsRequest request, CancellationToken ct)
    {

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var permissions = new List<object>();
        await foreach (var perm in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/permissions", ct))
        {
            permissions.Add(new
            {
                permission = perm.TryGetProperty("permission", out var p) ? p.GetString() : null,
                user = perm.TryGetObject("user", out var u) ? (object)new
                {
                    display_name = u.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                    username = u.TryGetProperty("username", out var un) ? un.GetString() : null,
                    account_id = u.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                } : null!,
                workspace = perm.TryGetObject("workspace", out var w) && w.TryGetProperty("slug", out var s) ? s.GetString() : null,
            });

            if (permissions.Count >= request.Limit) break;
        }

        return new { permissions, count = permissions.Count };
    }
}
