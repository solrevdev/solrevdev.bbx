using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.ViewSnippet;

public sealed class ViewSnippetHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewSnippetRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var snippet = await client.GetAsync<JsonElement>($"snippets/{workspace}/{request.SnippetId}", ct);

        return new
        {
            id = snippet.GetProperty("id").GetString(),
            title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
            is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
            scm = snippet.TryGetProperty("scm", out var s) ? s.GetString() : "git",
            created_on = snippet.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            updated_on = snippet.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
            owner = snippet.TryGetProperty("owner", out var o) ? (object)new
            {
                display_name = o.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                username = o.TryGetProperty("username", out var un) ? un.GetString() : null,
            } : null!,
            creator = snippet.TryGetProperty("creator", out var cr) ? (object)new
            {
                display_name = cr.TryGetProperty("display_name", out var cd) ? cd.GetString() : null,
                username = cr.TryGetProperty("username", out var cu) ? cu.GetString() : null,
            } : null!,
        };
    }
}
