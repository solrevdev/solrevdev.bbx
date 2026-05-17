using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;

namespace Bbx.Features.Workspaces.Projects.DeployKeys.ViewProjectDeployKey;

public sealed class ViewProjectDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewProjectDeployKeyRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");

        var key = await client.GetAsync<JsonElement>(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/deploy-keys/{request.KeyId}",
            ct);
        return ListProjectDeployKeysHandler.ProjectKey(key);
    }
}
