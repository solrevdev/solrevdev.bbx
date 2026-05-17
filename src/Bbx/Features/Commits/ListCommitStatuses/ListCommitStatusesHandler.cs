using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.ListCommitStatuses;

public sealed class ListCommitStatusesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListCommitStatusesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var statuses = new List<object>();
        await foreach (var status in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/statuses", ct))
        {
            statuses.Add(new
            {
                key = status.TryGetProperty("key", out var k) ? k.GetString() : null,
                state = status.TryGetProperty("state", out var s) ? s.GetString() : null,
                name = status.TryGetProperty("name", out var n) ? n.GetString() : null,
                url = status.TryGetProperty("url", out var u) ? u.GetString() : null,
                description = status.TryGetProperty("description", out var d) ? d.GetString() : null,
                created_on = status.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            });
        }

        return new { commit = request.Hash, count = statuses.Count, statuses };
    }
}
