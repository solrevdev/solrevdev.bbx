using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.ApproveCommit;

public sealed class ApproveCommitHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(ApproveCommitRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.PostAsync<object>($"/repositories/{ws}/{repo}/commit/{request.Hash}/approve", null, ct);
        return $"✓ Approved commit {request.Hash} in {ws}/{repo}";
    }
}
