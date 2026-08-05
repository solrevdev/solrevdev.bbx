using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestActivity;

public sealed class PullRequestActivityHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(PullRequestActivityRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        // With no id, the repository-wide feed. It is a separate endpoint, not
        // the per-pull-request one with a blank id, and it is unbounded, which
        // is why this one takes a limit.
        var endpoint = request.Id is null
            ? $"repositories/{ws}/{repo}/pullrequests/activity"
            : $"repositories/{ws}/{repo}/pullrequests/{request.Id}/activity";

        var activities = new List<JsonElement>();
        var count = 0;
        await foreach (var activity in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            activities.Add(activity);
            if (++count >= request.Limit) break;
        }

        return new { pull_request_id = request.Id, count = activities.Count, activities };
    }
}
