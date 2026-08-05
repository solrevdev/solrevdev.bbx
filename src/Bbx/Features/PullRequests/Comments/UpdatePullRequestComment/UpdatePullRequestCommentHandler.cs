using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Comments.UpdatePullRequestComment;

public sealed class UpdatePullRequestCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(UpdatePullRequestCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        // Only the comment text can change. Anchoring (path, line) is fixed at
        // creation, so the body carries content and nothing else.
        var body = new { content = new { raw = request.Body } };
        return await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/comments/{request.CommentId}", body, ct);
    }
}
