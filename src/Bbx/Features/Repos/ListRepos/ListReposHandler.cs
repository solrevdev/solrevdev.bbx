using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.ListRepos;

public sealed class ListReposHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListReposRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");

        var endpoint = $"/repositories/{workspace}";
        if (!string.IsNullOrEmpty(request.Query))
            endpoint += $"?q={Uri.EscapeDataString(request.Query)}";

        var repos = new List<object>();
        var count = 0;
        await foreach (var repo in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            repos.Add(new
            {
                name = repo.TryGetProperty("name", out var n) ? n.GetString() : null,
                slug = repo.TryGetProperty("slug", out var s) ? s.GetString() : null,
                full_name = repo.TryGetProperty("full_name", out var fn) ? fn.GetString() : null,
                is_private = repo.TryGetProperty("is_private", out var ip) && ip.GetBoolean(),
                scm = repo.TryGetProperty("scm", out var scm) ? scm.GetString() : null,
                description = repo.TryGetProperty("description", out var d) ? d.GetString() : null,
                updated_on = repo.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
                size = repo.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace,
            count = repos.Count,
            repositories = repos,
        };
    }
}
