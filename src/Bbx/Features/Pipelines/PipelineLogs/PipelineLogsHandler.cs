using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.PipelineLogs;

public sealed class PipelineLogsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(PipelineLogsRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var log = await client.GetRawAsync(
            $"repositories/{ws}/{repository}/pipelines/{request.PipelineUuid}/steps/{request.StepUuid}/log", ct);

        return new
        {
            pipeline_uuid = request.PipelineUuid,
            step_uuid = request.StepUuid,
            log,
        };
    }
}
