using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.UpdatePipelineKnownHost;

public sealed class UpdatePipelineKnownHostHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(UpdatePipelineKnownHostRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // This PUT replaces rather than merges, so the whole host key goes out
        // even when only the hostname is changing.
        var host = await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/known_hosts/{Uri.EscapeDataString(request.HostUuid)}",
            PipelineFormat.KnownHostBody(request.Hostname, request.KeyType, request.Key), ct);
        return PipelineFormat.KnownHost(host);
    }
}
