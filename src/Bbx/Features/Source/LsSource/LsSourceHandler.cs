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

        var path = EndpointPath.EscapeSegments(request.Path);

        // With no ref, Bitbucket serves the root of the main branch from the
        // bare /src endpoint by redirecting to src/{commit}/. That saves the
        // caller having to look up whether the branch is called main or master.
        // A path without a ref has nothing to hang off, so it is refused.
        string endpoint;
        if (string.IsNullOrEmpty(request.Ref))
        {
            if (!string.IsNullOrEmpty(request.Path))
                throw new BbxUserException("Error: --ref is required when a path is given.");
            endpoint = $"repositories/{ws}/{repo}/src";
        }
        else
        {
            endpoint = $"repositories/{ws}/{repo}/src/{Uri.EscapeDataString(request.Ref)}/{path}";
        }

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
            @ref = request.Ref ?? "(main branch)",
            path = request.Path ?? "",
            count = entries.Count,
            entries,
        };
    }

}
