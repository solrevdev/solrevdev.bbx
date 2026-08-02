using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.StopPipeline;

public sealed class StopPipelineHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(StopPipelineRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines/{request.PipelineUuid}/stopPipeline",
            new { }, ct);

        return new { message = "Pipeline stop requested", pipeline_uuid = request.PipelineUuid };
    }
}
