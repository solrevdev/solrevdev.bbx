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

        var activities = new List<JsonElement>();
        await foreach (var activity in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/activity", ct))
        {
            activities.Add(activity);
        }

        return new { pull_request_id = request.Id, count = activities.Count, activities };
    }
}
