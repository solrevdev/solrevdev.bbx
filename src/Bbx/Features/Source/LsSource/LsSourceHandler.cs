using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Source.LsSource;

public sealed class LsSourceHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(LsSourceRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var path = NormalizePath(request.Path);
        var refSegment = Uri.EscapeDataString(request.Ref);
        var endpoint = $"/repositories/{ws}/{repo}/src/{refSegment}/{path}";

        var entries = new List<object>();
        var count = 0;
        await foreach (var entry in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            entries.Add(new
            {
                type = entry.TryGetProperty("type", out var t) ? t.GetString() : null,
                path = entry.TryGetProperty("path", out var p) ? p.GetString() : null,
                size = entry.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Number
                    ? s.GetInt64()
                    : (long?)null,
                commit = entry.TryGetObject("commit", out var c) && c.TryGetProperty("hash", out var h)
                    ? h.GetString()
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            repository = repo,
            @ref = request.Ref,
            path = request.Path ?? "",
            count = entries.Count,
            entries,
        };
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        var trimmed = path.TrimStart('/');
        // Escape segments individually so '/' separators survive but
        // characters inside segments (spaces, %, etc.) get encoded.
        var parts = trimmed.Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Uri.EscapeDataString(parts[i]);
        }
        return string.Join('/', parts);
    }
}
