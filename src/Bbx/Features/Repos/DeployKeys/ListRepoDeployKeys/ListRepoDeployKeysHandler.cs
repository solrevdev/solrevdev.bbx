using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;

public sealed class ListRepoDeployKeysHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListRepoDeployKeysRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var keys = new List<object>();
        var count = 0;
        await foreach (var key in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/deploy-keys", ct))
        {
            keys.Add(ProjectKey(key));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = keys.Count, deploy_keys = keys };
    }

    internal static object ProjectKey(JsonElement k) => new
    {
        id = k.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.Number ? i.GetInt32() : (int?)null,
        label = k.TryGetProperty("label", out var l) ? l.GetString() : null,
        key = k.TryGetProperty("key", out var key) ? key.GetString() : null,
        comment = k.TryGetProperty("comment", out var c) ? c.GetString() : null,
        added_on = k.TryGetProperty("added_on", out var ao) ? ao.GetString() : null,
        last_used = k.TryGetProperty("last_used", out var lu) ? lu.GetString() : null,
    };
}
