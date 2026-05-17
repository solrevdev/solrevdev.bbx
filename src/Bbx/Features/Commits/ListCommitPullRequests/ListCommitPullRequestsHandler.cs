using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.ListCommitPullRequests;

public sealed class ListCommitPullRequestsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListCommitPullRequestsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var prs = new List<object>();
        await foreach (var pr in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/pullrequests", ct))
        {
            prs.Add(new
            {
                id = pr.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                title = pr.TryGetProperty("title", out var t) ? t.GetString() : null,
                state = pr.TryGetProperty("state", out var s) ? s.GetString() : null,
            });
        }

        return new { commit = request.Hash, count = prs.Count, pull_requests = prs };
    }
}
