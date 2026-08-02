using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Hooks.DeleteRepoHook;

public sealed class DeleteRepoHookHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteRepoHookRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/hooks/{Uri.EscapeDataString(request.Uid)}", ct);
        return $"✓ Deleted webhook '{request.Uid}' from {ws}/{repo}";
    }
}
