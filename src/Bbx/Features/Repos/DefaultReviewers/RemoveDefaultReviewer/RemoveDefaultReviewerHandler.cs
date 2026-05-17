using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DefaultReviewers.RemoveDefaultReviewer;

public sealed class RemoveDefaultReviewerHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RemoveDefaultReviewerRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Target))
            throw new BbxUserException("Error: --target (account ID or UUID) is required.");

        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/default-reviewers/{Uri.EscapeDataString(request.Target)}", ct);
        return $"✓ Removed default reviewer '{request.Target}' from {ws}/{repo}";
    }
}
