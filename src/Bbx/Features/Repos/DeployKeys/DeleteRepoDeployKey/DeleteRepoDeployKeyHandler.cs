using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DeployKeys.DeleteRepoDeployKey;

public sealed class DeleteRepoDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteRepoDeployKeyRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync($"/repositories/{ws}/{repo}/deploy-keys/{request.KeyId}", ct);
        return $"✓ Deleted deploy key #{request.KeyId} from {ws}/{repo}";
    }
}
