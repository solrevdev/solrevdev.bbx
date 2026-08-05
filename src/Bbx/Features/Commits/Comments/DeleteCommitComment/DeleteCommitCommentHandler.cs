using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.Comments.DeleteCommitComment;

public sealed class DeleteCommitCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteCommitCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/commit/{request.Hash}/comments/{request.CommentId}", ct);
        return $"✓ Deleted comment #{request.CommentId} on commit {request.Hash}";
    }
}
