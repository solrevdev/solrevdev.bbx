using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ClearAllPipelineCaches;

public sealed class ClearAllPipelineCachesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ClearAllPipelineCachesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        // DELETE on the collection is not "clear everything": it takes a name
        // in the query string and clears every cache with that name, whatever
        // its UUID. Without the parameter it answers a bare 400.
        // Verified live on 2026-08-05.
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/pipelines-config/caches?name={Uri.EscapeDataString(request.Name)}", ct);
        return new { message = $"Cleared every pipeline cache named '{request.Name}'", workspace = ws, repository = repo };
    }
}
