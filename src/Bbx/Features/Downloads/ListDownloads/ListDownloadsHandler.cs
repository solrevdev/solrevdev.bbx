using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Downloads.ListDownloads;

public sealed class ListDownloadsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListDownloadsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var downloads = new List<object>();
        var count = 0;
        await foreach (var dl in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/downloads", ct))
        {
            downloads.Add(new
            {
                name = dl.TryGetProperty("name", out var n) ? n.GetString() : null,
                size = dl.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Number
                    ? s.GetInt64()
                    : (long?)null,
                downloads = dl.TryGetProperty("downloads", out var d) && d.ValueKind == JsonValueKind.Number
                    ? d.GetInt32()
                    : (int?)null,
                created_on = dl.TryGetProperty("created_on", out var co) ? co.GetString() : null,
                user = dl.TryGetProperty("user", out var u) && u.TryGetProperty("display_name", out var dn)
                    ? dn.GetString()
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = downloads.Count, downloads };
    }
}
