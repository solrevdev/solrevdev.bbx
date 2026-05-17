using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;

public sealed class ListProjectDeployKeysHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListProjectDeployKeysRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");

        var keys = new List<object>();
        var count = 0;
        await foreach (var k in client.GetPaginatedAsync<JsonElement>(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/deploy-keys", ct))
        {
            keys.Add(ProjectKey(k));
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            project_key = request.ProjectKey,
            count = keys.Count,
            deploy_keys = keys,
        };
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
