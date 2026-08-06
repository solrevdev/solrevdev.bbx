using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestConflicts;

public sealed class PullRequestConflictsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(PullRequestConflictsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var conflicts = new List<JsonElement>();
        var count = 0;
        await foreach (var conflict in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/conflicts", ct))
        {
            conflicts.Add(conflict);
            if (++count >= request.Limit) break;
        }

        return new { pull_request_id = request.Id, count = conflicts.Count, conflicts };
    }
}
