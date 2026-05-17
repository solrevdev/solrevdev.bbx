using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.ListPullRequests;

public sealed class ListPullRequestsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPullRequestsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required. Use --workspace and --repo options.");

        var endpoint = $"/repositories/{ws}/{repo}/pullrequests";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(request.State)) queryParams.Add($"state={request.State.ToUpper()}");
        if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

        var prs = new List<object>();
        var count = 0;
        await foreach (var pr in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            prs.Add(PrFormatter.Summary(pr));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = prs.Count, pull_requests = prs };
    }
}

internal static class PrFormatter
{
    public static object Summary(JsonElement pr) => new
    {
        id = pr.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
        title = pr.TryGetProperty("title", out var title) ? title.GetString() : null,
        state = pr.TryGetProperty("state", out var state) ? state.GetString() : null,
        author = pr.TryGetProperty("author", out var author) && author.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
        source = pr.TryGetProperty("source", out var src) && src.TryGetProperty("branch", out var sb) && sb.TryGetProperty("name", out var sn) ? sn.GetString() : null,
        destination = pr.TryGetProperty("destination", out var dest) && dest.TryGetProperty("branch", out var db) && db.TryGetProperty("name", out var destName) ? destName.GetString() : null,
        created_on = pr.TryGetProperty("created_on", out var co) ? co.GetString() : null,
        updated_on = pr.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
        comment_count = pr.TryGetProperty("comment_count", out var cc) ? cc.GetInt32() : 0,
        task_count = pr.TryGetProperty("task_count", out var tc) ? tc.GetInt32() : 0,
    };
}
