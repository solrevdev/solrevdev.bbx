using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;

namespace Bbx.Features.Repos.DeployKeys.AddRepoDeployKey;

public sealed class AddRepoDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddRepoDeployKeyRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrWhiteSpace(request.Key))
            throw new BbxUserException(
                "Error: --key (the public SSH key, e.g. 'ssh-ed25519 AAAA...') is required.");

        var body = new Dictionary<string, object> { ["key"] = request.Key };
        if (!string.IsNullOrEmpty(request.Label)) body["label"] = request.Label;

        var key = await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/deploy-keys", body, ct);
        return ListRepoDeployKeysHandler.ProjectKey(key);
    }
}
