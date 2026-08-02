using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestDiff;

public sealed class PullRequestDiffHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(PullRequestDiffRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetStringAsync($"/repositories/{ws}/{repo}/pullrequests/{request.Id}/diff", ct);
    }
}
