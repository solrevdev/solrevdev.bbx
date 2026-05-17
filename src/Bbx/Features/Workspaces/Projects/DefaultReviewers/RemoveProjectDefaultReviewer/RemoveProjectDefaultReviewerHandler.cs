using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.RemoveProjectDefaultReviewer;

public sealed class RemoveProjectDefaultReviewerHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RemoveProjectDefaultReviewerRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        if (string.IsNullOrEmpty(request.Target))
            throw new BbxUserException("Error: --target (account ID or UUID) is required.");

        await client.DeleteAsync(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/default-reviewers/{Uri.EscapeDataString(request.Target)}",
            ct);
        return $"✓ Removed default reviewer '{request.Target}' from project {ws}/{request.ProjectKey}";
    }
}
