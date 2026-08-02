using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Snippets.ListSnippets;

public sealed class ListSnippetsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListSnippetsRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var endpoint = !string.IsNullOrEmpty(request.Workspace) ? $"snippets/{request.Workspace}" : "snippets";
        if (!string.IsNullOrEmpty(request.Role))
            endpoint += $"?role={request.Role}";

        var snippets = new List<object>();
        await foreach (var snippet in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            snippets.Add(new
            {
                id = snippet.GetProperty("id").GetString(),
                title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
                is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                scm = snippet.TryGetProperty("scm", out var s) ? s.GetString() : "git",
                created_on = snippet.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                updated_on = snippet.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
                owner = snippet.TryGetObject("owner", out var o) && o.TryGetProperty("display_name", out var d) ? d.GetString() : null,
            });

            if (snippets.Count >= request.Limit) break;
        }

        return new { snippets, count = snippets.Count };
    }
}
