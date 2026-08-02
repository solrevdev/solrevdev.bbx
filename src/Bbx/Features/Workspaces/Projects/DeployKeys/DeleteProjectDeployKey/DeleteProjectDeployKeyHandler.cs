using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DeployKeys.DeleteProjectDeployKey;

public sealed class DeleteProjectDeployKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteProjectDeployKeyRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");

        await client.DeleteAsync(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/deploy-keys/{request.KeyId}",
            ct);
        return $"✓ Deleted deploy key #{request.KeyId} from project {ws}/{request.ProjectKey}";
    }
}
