using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.ListIssueComments;

public sealed class ListIssueCommentsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListIssueCommentsRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var comments = new List<object>();
        await foreach (var comment in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/issues/{request.Id}/comments", ct))
        {
            comments.Add(new
            {
                id = comment.TryGetProperty("id", out var cid) ? cid.GetInt32() : 0,
                user = comment.TryGetObject("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                content = comment.TryGetObject("content", out var c) && c.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
                created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            });
        }

        return new { issue_id = request.Id, count = comments.Count, comments };
    }
}
