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
        await client.DeleteAsync($"repositories/{ws}/{repo}/pipelines-config/caches", ct);
        return new { message = "All pipeline caches cleared", workspace = ws, repository = repo };
    }
}
