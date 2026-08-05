using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Comments.ViewPullRequestComment;

public sealed class ViewPullRequestCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewPullRequestCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/comments/{request.CommentId}", ct);
    }
}
