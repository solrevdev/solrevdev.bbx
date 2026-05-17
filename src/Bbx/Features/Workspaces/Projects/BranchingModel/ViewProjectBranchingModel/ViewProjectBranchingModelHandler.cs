using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.BranchingModel.ViewProjectBranchingModel;

public sealed class ViewProjectBranchingModelHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewProjectBranchingModelRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        return await client.GetAsync<JsonElement>(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/branching-model", ct);
    }
}
