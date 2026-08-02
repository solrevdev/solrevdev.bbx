using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.ListCommits;

public sealed class ListCommitsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListCommitsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint = !string.IsNullOrEmpty(request.Branch)
            ? $"/repositories/{ws}/{repo}/commits/{Uri.EscapeDataString(request.Branch)}"
            : $"/repositories/{ws}/{repo}/commits";

        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(request.Path)) queryParams.Add($"path={Uri.EscapeDataString(request.Path)}");
        if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

        var commits = new List<object>();
        var count = 0;
        await foreach (var commit in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            commits.Add(CommitFormatter.Summary(commit));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = commits.Count, commits };
    }
}

internal static class CommitFormatter
{
    public static object Summary(JsonElement commit) => new
    {
        hash = commit.TryGetProperty("hash", out var h) ? h.GetString()?[..12] : null,
        full_hash = commit.TryGetProperty("hash", out var fh) ? fh.GetString() : null,
        message = commit.TryGetProperty("message", out var m) ? m.GetString()?.Split('\n')[0] : null,
        author = commit.TryGetObject("author", out var a) && a.TryGetObject("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() :
                 commit.TryGetObject("author", out var a2) && a2.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
        date = commit.TryGetProperty("date", out var d) ? d.GetString() : null,
    };
}
