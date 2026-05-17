using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.FileHistory;

public sealed class FileHistoryHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(FileHistoryRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Path))
            throw new BbxUserException("Error: <path> is required.");

        var path = NormalizePath(request.Path);
        var endpoint = $"/repositories/{ws}/{repo}/filehistory/{Uri.EscapeDataString(request.Hash)}/{path}";

        var entries = new List<object>();
        var count = 0;
        await foreach (var entry in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            entries.Add(new
            {
                commit = entry.TryGetProperty("commit", out var c) && c.TryGetProperty("hash", out var h)
                    ? h.GetString()
                    : null,
                path = entry.TryGetProperty("path", out var p) ? p.GetString() : null,
                type = entry.TryGetProperty("type", out var t) ? t.GetString() : null,
                size = entry.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Number
                    ? s.GetInt64()
                    : (long?)null,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            repository = repo,
            commit = request.Hash,
            path = request.Path,
            count = entries.Count,
            entries,
        };
    }

    private static string NormalizePath(string path)
    {
        var trimmed = path.TrimStart('/');
        var parts = trimmed.Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Uri.EscapeDataString(parts[i]);
        }
        return string.Join('/', parts);
    }
}
