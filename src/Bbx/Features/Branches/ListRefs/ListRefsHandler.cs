using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.ListRefs;

public sealed class ListRefsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListRefsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        // Branches and tags in one list. `branch list` and `branch tag list`
        // each read one half of it.
        var endpoint = $"repositories/{ws}/{repo}/refs";
        if (!string.IsNullOrEmpty(request.Query))
            endpoint += $"?q={Uri.EscapeDataString(request.Query)}";

        var refs = new List<object>();
        var count = 0;
        await foreach (var item in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            refs.Add(new
            {
                type = item.GetStringOrNull("type"),
                name = item.GetStringOrNull("name"),
                target = item.TryGetObject("target", out var t) ? t.GetStringOrNull("hash") : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = refs.Count, refs };
    }
}
