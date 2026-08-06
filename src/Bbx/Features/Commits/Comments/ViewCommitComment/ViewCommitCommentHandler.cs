using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.Comments.ViewCommitComment;

public sealed class ViewCommitCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewCommitCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/commit/{request.Hash}/comments/{request.CommentId}", ct);
    }
}
