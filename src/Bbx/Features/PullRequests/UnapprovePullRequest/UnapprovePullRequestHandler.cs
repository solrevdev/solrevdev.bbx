using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.UnapprovePullRequest;

public sealed class UnapprovePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(UnapprovePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync($"/repositories/{ws}/{repo}/pullrequests/{request.Id}/approve", ct);
        return $"✓ Removed approval from PR #{request.Id}";
    }
}
