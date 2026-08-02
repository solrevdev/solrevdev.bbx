using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.ListWatchers;

public sealed class ListWatchersHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWatchersRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var watchers = new List<object>();
        var count = 0;
        await foreach (var watcher in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/watchers", ct))
        {
            watchers.Add(new
            {
                uuid = watcher.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                account_id = watcher.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                nickname = watcher.TryGetProperty("nickname", out var n) ? n.GetString() : null,
                display_name = watcher.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                type = watcher.TryGetProperty("type", out var t) ? t.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = watchers.Count, watchers };
    }
}
