using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewPipelineKnownHost;

public sealed class ViewPipelineKnownHostHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewPipelineKnownHostRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var host = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/known_hosts/{Uri.EscapeDataString(request.HostUuid)}", ct);
        return PipelineFormat.KnownHost(host);
    }
}
