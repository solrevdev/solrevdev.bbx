using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.Comments.UpdateCommitComment;

public sealed class UpdateCommitCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(UpdateCommitCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        // Anchoring is fixed at creation, so only the text can change.
        var body = new { content = new { raw = request.Body } };
        return await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/commit/{request.Hash}/comments/{request.CommentId}", body, ct);
    }
}
