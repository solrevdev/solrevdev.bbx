using System.Text.Json;
using Bbx.Api;

namespace Bbx.Features.Users.ListUserWorkspaces;

public sealed class ListUserWorkspacesHandler(BitbucketClient client)
{
    public async Task<object> HandleAsync(ListUserWorkspacesRequest request, CancellationToken ct)
    {
        // The account-scoped list. /2.0/workspaces, which `workspace list`
        // calls, is 410 Gone.
        var workspaces = new List<object>();
        var count = 0;
        await foreach (var w in client.GetPaginatedAsync<JsonElement>("user/workspaces", ct))
        {
            // Each entry is a workspace_access record, not a workspace: the
            // slug and uuid sit one level down and the top level carries only
            // the caller's role. Reading it flat gave three rows of nulls.
            // The nested record is a workspace_base and carries no name.
            // Verified against the live API on 2026-08-05.
            var workspace = w.TryGetObject("workspace", out var nested) ? nested : w;
            workspaces.Add(new
            {
                slug = workspace.GetStringOrNull("slug"),
                uuid = workspace.GetStringOrNull("uuid"),
                administrator = w.TryGetProperty("administrator", out var admin)
                    && admin.ValueKind is JsonValueKind.True,
            });
            if (++count >= request.Limit) break;
        }

        return new { count = workspaces.Count, workspaces };
    }
}
