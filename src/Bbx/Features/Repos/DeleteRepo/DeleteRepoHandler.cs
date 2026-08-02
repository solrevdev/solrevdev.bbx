using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DeleteRepo;

public sealed class DeleteRepoHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteRepoRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.RepoPath(credentials, request.Workspace, request.Repository,
            "Error: Invalid repository path.");
        await client.DeleteAsync($"/repositories/{ws}/{repo}", ct);
        return $"✓ Deleted {ws}/{repo}";
    }
}
