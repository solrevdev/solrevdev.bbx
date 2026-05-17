using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.CommitPatch;

public sealed class CommitPatchHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(CommitPatchRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetStringAsync($"/repositories/{ws}/{repo}/patch/{request.Hash}", ct);
    }
}
