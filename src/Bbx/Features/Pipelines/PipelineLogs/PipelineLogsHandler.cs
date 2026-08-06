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

        var step = $"repositories/{ws}/{repository}/pipelines/{request.PipelineUuid}/steps/{request.StepUuid}";
        // The numbered logs live under a different path from the step's own
        // log, not behind a query parameter on it.
        var endpoint = string.IsNullOrEmpty(request.LogUuid)
            ? $"{step}/log"
            : $"{step}/logs/{Uri.EscapeDataString(request.LogUuid)}";

        var log = await client.GetRawAsync(endpoint, ct);

        return new
        {
            pipeline_uuid = request.PipelineUuid,
            step_uuid = request.StepUuid,
            log_uuid = request.LogUuid,
            log,
        };
    }
}
