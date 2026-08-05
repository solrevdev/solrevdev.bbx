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

        // An absent array option parses to an empty array rather than null, so
        // both are tested for length. Length zero means the caller did not ask
        // for a walk and the plain GET still applies.
        var include = request.Include ?? [];
        var exclude = request.Exclude ?? [];

        var commits = include.Length > 0 || exclude.Length > 0
            ? await WalkAsync(endpoint, include, exclude, request.Limit, ct)
            : await ListAsync(endpoint, request.Limit, ct);

        return new { workspace = ws, repository = repo, count = commits.Count, commits };
    }

    private async Task<List<object>> ListAsync(string endpoint, int limit, CancellationToken ct)
    {
        var commits = new List<object>();
        var count = 0;
        await foreach (var commit in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            commits.Add(CommitFormatter.Summary(commit));
            if (++count >= limit) break;
        }
        return commits;
    }

    /// <summary>
    /// Walk the commits reachable from <paramref name="include"/> but not from
    /// <paramref name="exclude"/>. Bitbucket takes those two lists in a POST
    /// body only, so this cannot reuse the paginating GET helper.
    /// </summary>
    private async Task<List<object>> WalkAsync(
        string endpoint, string[] include, string[] exclude, int limit, CancellationToken ct)
    {
        var body = new Dictionary<string, object>();
        if (include.Length > 0) body["include"] = include;
        if (exclude.Length > 0) body["exclude"] = exclude;

        var commits = new List<object>();
        var page = await client.PostAsync<PaginatedResponse<JsonElement>>(endpoint, body, ct);
        while (page is not null)
        {
            foreach (var commit in page.Values)
            {
                commits.Add(CommitFormatter.Summary(commit));
                if (commits.Count >= limit) return commits;
            }
            // The next link carries the walk in its query string, so following
            // it is a plain GET even though the first page was a POST.
            if (string.IsNullOrEmpty(page.Next)) break;
            page = await client.GetAsync<PaginatedResponse<JsonElement>>(page.Next, ct);
        }

        return commits;
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
