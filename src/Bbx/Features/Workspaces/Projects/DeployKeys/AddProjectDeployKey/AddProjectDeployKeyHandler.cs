using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;

namespace Bbx.Features.Workspaces.Projects.DeployKeys.AddProjectDeployKey;

public sealed class AddProjectDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddProjectDeployKeyRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        if (string.IsNullOrWhiteSpace(request.Key))
            throw new BbxUserException("Error: --key (the public SSH key body) is required.");

        var body = new Dictionary<string, object> { ["key"] = request.Key };
        if (!string.IsNullOrEmpty(request.Label)) body["label"] = request.Label;

        var key = await client.PostAsync<JsonElement>(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/deploy-keys", body, ct);
        return ListProjectDeployKeysHandler.ProjectKey(key);
    }
}
