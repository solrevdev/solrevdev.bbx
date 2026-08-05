using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.PipelineCacheContentUri;

public sealed class PipelineCacheContentUriHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(PipelineCacheContentUriRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        // A short-lived signed URL for the cache archive, not the archive
        // itself, so this stays a plain JSON read.
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines-config/caches/{Uri.EscapeDataString(request.CacheUuid)}/content-uri", ct);
    }
}
