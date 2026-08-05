using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeletePipelineKnownHost;

public sealed class DeletePipelineKnownHostHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeletePipelineKnownHostRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/known_hosts/{Uri.EscapeDataString(request.HostUuid)}", ct);
        return $"\u2713 Deleted known host {request.HostUuid}";
    }
}
