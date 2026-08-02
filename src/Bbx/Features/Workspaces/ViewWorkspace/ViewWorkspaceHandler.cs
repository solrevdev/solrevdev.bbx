using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ViewWorkspace;

public sealed class ViewWorkspaceHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewWorkspaceRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Provide as argument or set default with 'bbx auth set-workspace'.");

        var ws = await client.GetAsync<JsonElement>($"workspaces/{workspace}", ct);

        return new
        {
            slug = ws.TryGetProperty("slug", out var s) ? s.GetString() : null,
            name = ws.TryGetProperty("name", out var n) ? n.GetString() : null,
            uuid = ws.TryGetProperty("uuid", out var u) ? u.GetString() : null,
            is_private = ws.TryGetProperty("is_private", out var p) && p.GetBoolean(),
            created_on = ws.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            links = ws.TryGetProperty("links", out var l) ? (object)new
            {
                html = l.TryGetObject("html", out var h) && h.TryGetProperty("href", out var href) ? href.GetString() : null,
                avatar = l.TryGetObject("avatar", out var a) && a.TryGetProperty("href", out var ahref) ? ahref.GetString() : null,
            } : null!,
        };
    }
}
