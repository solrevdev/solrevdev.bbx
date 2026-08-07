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

        if (request.UpdateId.HasValue)
        {
            if (string.IsNullOrEmpty(request.UpdateContent))
                throw new BbxUserException("Error: --update needs --content to say what the comment should say.");

            var updated = await client.PutAsync<JsonElement>(
                $"snippets/{workspace}/{request.SnippetId}/comments/{request.UpdateId}",
                new { content = new { raw = request.UpdateContent } }, ct);

            return new
            {
                id = updated.TryGetProperty("id", out var ui) ? ui.GetInt32() : request.UpdateId.Value,
                content = updated.TryGetObject("content", out var uc) && uc.TryGetProperty("raw", out var ur)
                    ? ur.GetString() : null,
                updated_on = updated.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
            };
        }

        if (request.DeleteId.HasValue)
        {
            await client.DeleteAsync($"snippets/{workspace}/{request.SnippetId}/comments/{request.DeleteId}", ct);
            return new { deleted = true, comment_id = request.DeleteId };
        }

        if (request.ViewId.HasValue)
        {
            var one = await client.GetAsync<JsonElement>(
                $"snippets/{workspace}/{request.SnippetId}/comments/{request.ViewId}", ct);

            return new
            {
                id = one.TryGetProperty("id", out var vi) ? vi.GetInt32() : request.ViewId.Value,
                content = one.TryGetObject("content", out var vc) && vc.TryGetProperty("raw", out var vr)
                    ? vr.GetString() : null,
                user = one.TryGetObject("user", out var vu) && vu.TryGetProperty("display_name", out var vd)
                    ? vd.GetString() : null,
                created_on = one.TryGetProperty("created_on", out var vco) ? vco.GetString() : null,
                updated_on = one.TryGetProperty("updated_on", out var vuo) ? vuo.GetString() : null,
                deleted = one.TryGetProperty("deleted", out var vdel) && vdel.ValueKind == JsonValueKind.True,
            };
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
                deleted = comment.TryGetProperty("deleted", out var del) && del.ValueKind == JsonValueKind.True,
            });
        }

        return new { comments, count = comments.Count };
    }
}
