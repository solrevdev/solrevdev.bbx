using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestMergeStatus;

public sealed class PullRequestMergeStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(PullRequestMergeStatusRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        // A merge asked for asynchronously answers 202 with a task ID. This
        // reports whether that task has finished.
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/merge/task-status/{Uri.EscapeDataString(request.TaskId)}",
            ct);
    }
}
