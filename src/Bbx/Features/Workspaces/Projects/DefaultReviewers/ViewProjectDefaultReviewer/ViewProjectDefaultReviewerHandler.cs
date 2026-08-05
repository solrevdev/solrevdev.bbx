using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.ViewProjectDefaultReviewer;

public sealed class ViewProjectDefaultReviewerHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewProjectDefaultReviewerRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);
        return await client.GetAsync<JsonElement>(
            $"workspaces/{workspace}/projects/{key}/default-reviewers/{Uri.EscapeDataString(request.Target)}", ct);
    }
}
