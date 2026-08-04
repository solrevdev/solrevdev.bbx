using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.SnippetComments;

public sealed class SnippetCommentsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(SnippetCommentsRequest request, CancellationToken ct)
    {

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (!string.IsNullOrEmpty(request.AddContent))
        {
            var comment = await client.PostAsync<JsonElement>(
                $"snippets/{workspace}/{request.SnippetId}/comments",
                new { content = new { raw = request.AddContent } }, ct);

            return new
            {
                id = comment.GetProperty("id").GetInt32(),
                content = comment.TryGetObject("content", out var c) && c.TryGetProperty("raw", out var r) ? r.GetString() : null,
                created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            };
        }

        if (request.DeleteId.HasValue)
        {
            await client.DeleteAsync($"snippets/{workspace}/{request.SnippetId}/comments/{request.DeleteId}", ct);
            return new { deleted = true, comment_id = request.DeleteId };
        }

        var comments = new List<object>();
        await foreach (var comment in client.GetPaginatedAsync<JsonElement>(
            $"snippets/{workspace}/{request.SnippetId}/comments", ct))
        {
            comments.Add(new
            {
                id = comment.GetProperty("id").GetInt32(),
                content = comment.TryGetObject("content", out var c) && c.TryGetProperty("raw", out var r) ? r.GetString() : null,
                user = comment.TryGetObject("user", out var u) && u.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            });
        }

        return new { comments, count = comments.Count };
    }
}
