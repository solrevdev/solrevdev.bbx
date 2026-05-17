using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Tasks.DeletePullRequestTask;

public sealed class DeletePullRequestTaskHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeletePullRequestTaskRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/tasks/{request.TaskId}", ct);
        return $"✓ Deleted task #{request.TaskId} from PR #{request.Id}";
    }
}
