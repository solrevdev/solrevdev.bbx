using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.CommitDiff;

public sealed class CommitDiffHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(CommitDiffRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetStringAsync($"/repositories/{ws}/{repo}/diff/{request.Hash}", ct);
    }
}
