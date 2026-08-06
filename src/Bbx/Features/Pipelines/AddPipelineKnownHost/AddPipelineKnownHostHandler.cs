using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.AddPipelineKnownHost;

public sealed class AddPipelineKnownHostHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddPipelineKnownHostRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var host = await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/known_hosts",
            PipelineFormat.KnownHostBody(request.Hostname, request.KeyType, request.Key), ct);
        return PipelineFormat.KnownHost(host);
    }
}
