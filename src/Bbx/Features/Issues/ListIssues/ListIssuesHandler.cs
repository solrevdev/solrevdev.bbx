using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.ListIssues;

public sealed class ListIssuesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListIssuesRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();

        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint = $"/repositories/{ws}/{repo}/issues";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(request.State)) queryParams.Add($"q=state=\"{request.State}\"");
        if (!string.IsNullOrEmpty(request.Priority)) queryParams.Add($"priority=\"{request.Priority}\"");
        if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

        var issues = new List<object>();
        var count = 0;
        await foreach (var issue in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            issues.Add(IssueFormatter.Summary(issue));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = issues.Count, issues };
    }
}

internal static class IssueFormatter
{
    public static object Summary(JsonElement issue) => new
    {
        id = issue.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
        title = issue.TryGetProperty("title", out var t) ? t.GetString() : null,
        state = issue.TryGetProperty("state", out var s) ? s.GetString() : null,
        kind = issue.TryGetProperty("kind", out var k) ? k.GetString() : null,
        priority = issue.TryGetProperty("priority", out var p) ? p.GetString() : null,
        reporter = issue.TryGetObject("reporter", out var r) && r.TryGetProperty("display_name", out var rdn) ? rdn.GetString() : null,
        assignee = issue.TryGetObject("assignee", out var a) && a.TryGetProperty("display_name", out var adn) ? adn.GetString() : null,
        created_on = issue.TryGetProperty("created_on", out var co) ? co.GetString() : null,
        updated_on = issue.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
    };
}
