using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestPatch;

public sealed class PullRequestPatchHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(PullRequestPatchRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetStringAsync(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/patch", ct);
    }
}
