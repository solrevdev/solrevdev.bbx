using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ClearPipelineCache;

public sealed class ClearPipelineCacheHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ClearPipelineCacheRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        await client.DeleteAsync(
            $"repositories/{ws}/{repository}/pipelines-config/caches/{Uri.EscapeDataString(request.Name)}", ct);

        return new { message = "Cache cleared successfully", name = request.Name };
    }
}
