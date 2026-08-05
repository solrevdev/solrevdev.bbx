using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.SnippetCommits;

public sealed class SnippetCommitsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(SnippetCommitsRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (!string.IsNullOrEmpty(request.Revision))
        {
            var commit = await client.GetAsync<JsonElement>(
                $"snippets/{workspace}/{request.SnippetId}/commits/{Uri.EscapeDataString(request.Revision)}", ct);
            return new { snippet_id = request.SnippetId, commit };
        }

        var commits = new List<JsonElement>();
        var count = 0;
        await foreach (var c in client.GetPaginatedAsync<JsonElement>(
            $"snippets/{workspace}/{request.SnippetId}/commits", ct))
        {
            commits.Add(c);
            if (++count >= request.Limit) break;
        }

        return new { snippet_id = request.SnippetId, count = commits.Count, commits };
    }
}
