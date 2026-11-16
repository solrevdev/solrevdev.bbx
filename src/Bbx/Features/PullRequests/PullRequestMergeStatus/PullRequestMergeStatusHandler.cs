using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestMergeStatus;

public sealed class PullRequestMergeStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<CommandOutcome> HandleAsync(PullRequestMergeStatusRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var taskId = MergeTask.IdFrom(request.TaskId);

        if (request.Wait)
        {
            return await MergeTask.WaitAsync(client, ws, repo, request.Id, taskId, null,
                request.TimeoutSeconds, request.PollInterval ?? MergeTask.DefaultInterval, ct);
        }

        // Without --wait this reports the task as Bitbucket has it, PENDING
        // included, and exits 0: the caller asked for a status, not a result.
        return new CommandOutcome(
            await client.GetAsync<JsonElement>(MergeTask.Endpoint(ws, repo, request.Id, taskId), ct));
    }
}
