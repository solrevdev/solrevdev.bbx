using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.AddPullRequestComment;

public sealed class AddPullRequestCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(AddPullRequestCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var body = new { content = new { raw = request.Body } };
        return await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/comments", body, ct);
    }
}
