using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.FileConflicts;

public sealed class FileConflictsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(FileConflictsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint = $"repositories/{ws}/{repo}/file-conflicts/{Uri.EscapeDataString(request.Spec)}";
        var conflicts = new List<JsonElement>();
        var count = 0;
        await foreach (var conflict in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            conflicts.Add(conflict);
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, spec = request.Spec, count = conflicts.Count, conflicts };
    }
}
