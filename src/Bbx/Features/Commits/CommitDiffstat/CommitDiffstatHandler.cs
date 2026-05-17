using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.CommitDiffstat;

public sealed class CommitDiffstatHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(CommitDiffstatRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Spec))
            throw new BbxUserException(
                "Error: <spec> is required (commit hash, branch, or 'src..dst' range).");

        var endpoint = $"/repositories/{ws}/{repo}/diffstat/{Uri.EscapeDataString(request.Spec)}";

        var entries = new List<object>();
        var count = 0;
        await foreach (var entry in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            entries.Add(new
            {
                status = entry.TryGetProperty("status", out var s) ? s.GetString() : null,
                lines_added = entry.TryGetProperty("lines_added", out var la) && la.ValueKind == JsonValueKind.Number
                    ? la.GetInt32()
                    : (int?)null,
                lines_removed = entry.TryGetProperty("lines_removed", out var lr) && lr.ValueKind == JsonValueKind.Number
                    ? lr.GetInt32()
                    : (int?)null,
                old_path = entry.TryGetProperty("old", out var oldE) && oldE.ValueKind == JsonValueKind.Object
                           && oldE.TryGetProperty("path", out var op) ? op.GetString() : null,
                new_path = entry.TryGetProperty("new", out var newE) && newE.ValueKind == JsonValueKind.Object
                           && newE.TryGetProperty("path", out var np) ? np.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            repository = repo,
            spec = request.Spec,
            count = entries.Count,
            files = entries,
        };
    }
}
