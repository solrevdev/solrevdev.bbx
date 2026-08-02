using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;

namespace Bbx.Features.Repos.DeployKeys.ViewRepoDeployKey;

public sealed class ViewRepoDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewRepoDeployKeyRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var key = await client.GetAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/deploy-keys/{request.KeyId}", ct);
        return ListRepoDeployKeysHandler.ProjectKey(key);
    }
}
