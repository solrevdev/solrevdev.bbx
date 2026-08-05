using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;

namespace Bbx.Features.Repos.DeployKeys.UpdateRepoDeployKey;

public sealed class UpdateRepoDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(UpdateRepoDeployKeyRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        // Unlike the other PUTs, this one replaces rather than merges: the key
        // material has to be sent even when only the label is changing.
        var body = new Dictionary<string, object> { ["key"] = request.Key };
        if (request.Label is not null) body["label"] = request.Label;

        var key = await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/deploy-keys/{request.KeyId}", body, ct);
        return ListRepoDeployKeysHandler.ProjectKey(key);
    }
}
