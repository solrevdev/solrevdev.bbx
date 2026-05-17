using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DeleteProject;

public sealed class DeleteProjectHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteProjectRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        await client.DeleteAsync(
            $"workspaces/{workspace}/projects/{Uri.EscapeDataString(request.ProjectKey)}", ct);
        return $"✓ Deleted project '{request.ProjectKey}' from workspace {workspace}";
    }
}
