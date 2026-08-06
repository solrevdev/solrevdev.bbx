using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.UpdatePipelinesConfig;

public sealed class UpdatePipelinesConfigHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(UpdatePipelinesConfigRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        return await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config", new { enabled = request.Enabled }, ct);
    }
}
