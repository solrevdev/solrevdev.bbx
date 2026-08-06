using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Comments.ResolvePullRequestComment;

public sealed class ResolvePullRequestCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(ResolvePullRequestCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint =
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/comments/{request.CommentId}/resolve";

        if (request.Resolve)
        {
            await client.PostAsync<JsonElement>(endpoint, null, ct);
            return $"✓ Resolved comment #{request.CommentId} on PR #{request.Id}";
        }

        await client.DeleteAsync(endpoint, ct);
        return $"✓ Reopened comment #{request.CommentId} on PR #{request.Id}";
    }
}
