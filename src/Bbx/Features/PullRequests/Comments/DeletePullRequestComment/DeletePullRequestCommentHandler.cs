using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Comments.DeletePullRequestComment;

public sealed class DeletePullRequestCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeletePullRequestCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/comments/{request.CommentId}", ct);
        return $"✓ Deleted comment #{request.CommentId} on PR #{request.Id}";
    }
}
