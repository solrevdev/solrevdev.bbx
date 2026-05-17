using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.ListForks;

public sealed class ListForksHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListForksRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var forks = new List<object>();
        var count = 0;
        await foreach (var fork in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/forks", ct))
        {
            forks.Add(new
            {
                full_name = fork.TryGetProperty("full_name", out var fn) ? fn.GetString() : null,
                slug = fork.TryGetProperty("slug", out var s) ? s.GetString() : null,
                is_private = fork.TryGetProperty("is_private", out var ip) && ip.GetBoolean(),
                created_on = fork.TryGetProperty("created_on", out var co) ? co.GetString() : null,
                owner = fork.TryGetProperty("workspace", out var w) && w.TryGetProperty("slug", out var ws2)
                    ? ws2.GetString()
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = forks.Count, forks };
    }
}
