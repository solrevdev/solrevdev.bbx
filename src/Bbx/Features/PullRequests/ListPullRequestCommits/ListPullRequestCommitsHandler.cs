using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.ListPullRequestCommits;

public sealed class ListPullRequestCommitsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPullRequestCommitsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var commits = new List<object>();
        var count = 0;
        await foreach (var commit in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/commits", ct))
        {
            var hash = commit.TryGetProperty("hash", out var h) ? h.GetString() : null;
            commits.Add(new
            {
                hash,
                short_hash = hash is { Length: > 12 } ? hash[..12] : hash,
                message = commit.TryGetProperty("message", out var m) ? m.GetString() : null,
                date = commit.TryGetProperty("date", out var d) ? d.GetString() : null,
                author = commit.TryGetProperty("author", out var a) && a.TryGetProperty("raw", out var raw)
                    ? raw.GetString()
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { pull_request_id = request.Id, count = commits.Count, commits };
    }
}
