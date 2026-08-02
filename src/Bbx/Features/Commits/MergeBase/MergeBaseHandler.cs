using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.MergeBase;

public sealed class MergeBaseHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(MergeBaseRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Spec))
            throw new BbxUserException(
                "Error: <spec> is required (e.g., 'feature..main' or 'abc123..def456').");

        var commit = await client.GetAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/merge-base/{Uri.EscapeDataString(request.Spec)}", ct);

        return new
        {
            spec = request.Spec,
            hash = commit.TryGetProperty("hash", out var h) ? h.GetString() : null,
            date = commit.TryGetProperty("date", out var d) ? d.GetString() : null,
            message = commit.TryGetProperty("message", out var m) ? m.GetString() : null,
            author = commit.TryGetObject("author", out var a) && a.TryGetProperty("raw", out var raw)
                ? raw.GetString()
                : null,
        };
    }
}
