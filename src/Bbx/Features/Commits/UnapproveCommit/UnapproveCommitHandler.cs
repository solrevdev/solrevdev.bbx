using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.UnapproveCommit;

public sealed class UnapproveCommitHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(UnapproveCommitRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync($"/repositories/{ws}/{repo}/commit/{request.Hash}/approve", ct);
        return $"✓ Removed approval from commit {request.Hash} in {ws}/{repo}";
    }
}
