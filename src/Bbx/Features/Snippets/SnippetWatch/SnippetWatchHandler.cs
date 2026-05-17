using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.SnippetWatch;

public sealed class SnippetWatchHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(SnippetWatchRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (request.List)
        {
            var watchers = new List<object>();
            await foreach (var watcher in client.GetPaginatedAsync<JsonElement>(
                $"snippets/{workspace}/{request.SnippetId}/watchers", ct))
            {
                watchers.Add(new
                {
                    display_name = watcher.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                    username = watcher.TryGetProperty("username", out var u) ? u.GetString() : null,
                    account_id = watcher.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                });
            }
            return new { watchers, count = watchers.Count };
        }

        if (request.Unwatch)
        {
            await client.DeleteAsync($"snippets/{workspace}/{request.SnippetId}/watch", ct);
            return new { watching = false, snippet_id = request.SnippetId };
        }

        await client.PutAsync<JsonElement>($"snippets/{workspace}/{request.SnippetId}/watch", new { }, ct);
        return new { watching = true, snippet_id = request.SnippetId };
    }
}
