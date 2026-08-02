using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Workspaces.ListWorkspaces;

public sealed class ListWorkspacesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWorkspacesRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var endpoint = "workspaces";
        if (!string.IsNullOrEmpty(request.Role))
            endpoint += $"?role={request.Role}";

        var workspaces = new List<object>();
        await foreach (var ws in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            workspaces.Add(new
            {
                slug = ws.TryGetProperty("slug", out var s) ? s.GetString() : null,
                name = ws.TryGetProperty("name", out var n) ? n.GetString() : null,
                uuid = ws.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                is_private = ws.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                created_on = ws.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            });

            if (workspaces.Count >= request.Limit) break;
        }

        return new { workspaces, count = workspaces.Count };
    }
}
