using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ListWorkspaceRepositoryPermissions;

public sealed class ListWorkspaceRepositoryPermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWorkspaceRepositoryPermissionsRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var endpoint = $"workspaces/{workspace}/permissions/repositories";
        if (!string.IsNullOrEmpty(request.Repository))
            endpoint += $"/{Uri.EscapeDataString(request.Repository)}";

        var permissions = new List<object>();
        var count = 0;
        await foreach (var p in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            permissions.Add(new
            {
                permission = p.GetStringOrNull("permission"),
                repository = p.TryGetObject("repository", out var r) ? r.GetStringOrNull("full_name") : null,
                user = p.TryGetObject("user", out var u)
                    ? new { display_name = u.GetStringOrNull("display_name"), account_id = u.GetStringOrNull("account_id") }
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace, repository = request.Repository, count = permissions.Count, permissions };
    }
}
