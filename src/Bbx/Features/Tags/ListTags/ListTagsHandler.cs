using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Tags.ListTags;

public sealed class ListTagsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListTagsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint = $"/repositories/{ws}/{repo}/refs/tags";
        var query = new List<string>();
        if (!string.IsNullOrEmpty(request.Sort)) query.Add($"sort={request.Sort}");
        if (!string.IsNullOrEmpty(request.Query)) query.Add($"q={Uri.EscapeDataString(request.Query)}");
        if (query.Count > 0) endpoint += "?" + string.Join("&", query);

        var tags = new List<object>();
        var count = 0;
        await foreach (var tag in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            tags.Add(new
            {
                name = tag.TryGetProperty("name", out var n) ? n.GetString() : null,
                target = tag.TryGetObject("target", out var t) && t.TryGetProperty("hash", out var h)
                    ? h.GetString()?[..Math.Min(12, h.GetString()?.Length ?? 0)]
                    : null,
                tagger = tag.TryGetObject("tagger", out var tg) && tg.TryGetObject("user", out var u)
                         && u.TryGetProperty("display_name", out var dn)
                    ? dn.GetString()
                    : null,
                date = tag.TryGetProperty("date", out var d) ? d.GetString() : null,
                message = tag.TryGetProperty("message", out var m) ? m.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = tags.Count, tags };
    }
}
