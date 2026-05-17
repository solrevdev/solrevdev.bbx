using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestStatuses;

public sealed class PullRequestStatusesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(PullRequestStatusesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var statuses = new List<object>();
        await foreach (var status in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/statuses", ct))
        {
            statuses.Add(new
            {
                key = status.TryGetProperty("key", out var k) ? k.GetString() : null,
                state = status.TryGetProperty("state", out var s) ? s.GetString() : null,
                name = status.TryGetProperty("name", out var n) ? n.GetString() : null,
                url = status.TryGetProperty("url", out var u) ? u.GetString() : null,
                description = status.TryGetProperty("description", out var d) ? d.GetString() : null,
            });
        }

        return new { pull_request_id = request.Id, count = statuses.Count, statuses };
    }
}
