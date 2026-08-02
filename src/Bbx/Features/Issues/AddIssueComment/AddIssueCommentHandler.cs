using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.AddIssueComment;

public sealed class AddIssueCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(AddIssueCommentRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var body = new { content = new { raw = request.Body } };
        return await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/issues/{request.Id}/comments", body, ct);
    }
}
