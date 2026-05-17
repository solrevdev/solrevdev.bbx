using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.ListCommitComments;

public sealed class ListCommitCommentsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListCommitCommentsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var comments = new List<object>();
        await foreach (var comment in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/comments", ct))
        {
            comments.Add(new
            {
                id = comment.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                user = comment.TryGetProperty("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                content = comment.TryGetProperty("content", out var c) && c.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
                created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            });
        }

        return new { commit = request.Hash, count = comments.Count, comments };
    }
}
