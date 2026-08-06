using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestDiffstat;

public sealed class PullRequestDiffstatHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(PullRequestDiffstatRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var files = new List<object>();
        var added = 0;
        var removed = 0;
        var count = 0;
        await foreach (var entry in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pullrequests/{request.Id}/diffstat", ct))
        {
            var linesAdded = entry.TryGetProperty("lines_added", out var la) && la.ValueKind == JsonValueKind.Number
                ? la.GetInt32() : 0;
            var linesRemoved = entry.TryGetProperty("lines_removed", out var lr) && lr.ValueKind == JsonValueKind.Number
                ? lr.GetInt32() : 0;
            added += linesAdded;
            removed += linesRemoved;
            files.Add(new
            {
                status = entry.GetStringOrNull("status"),
                // A deleted file has no "new", an added file has no "old", and
                // both arrive as an explicit JSON null rather than as absent.
                path = entry.TryGetObject("new", out var n) ? n.GetStringOrNull("path")
                    : entry.TryGetObject("old", out var o) ? o.GetStringOrNull("path")
                    : null,
                lines_added = linesAdded,
                lines_removed = linesRemoved,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            pull_request_id = request.Id,
            count = files.Count,
            lines_added = added,
            lines_removed = removed,
            files,
        };
    }
}
