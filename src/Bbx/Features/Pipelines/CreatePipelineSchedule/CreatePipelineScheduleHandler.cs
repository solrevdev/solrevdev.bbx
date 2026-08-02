using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.CreatePipelineSchedule;

public sealed class CreatePipelineScheduleHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(CreatePipelineScheduleRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // "type" and "selector" are both required. Without the discriminator
        // Bitbucket answers "An invalid field was found in the JSON payload"
        // (it cannot resolve the target subtype), and without a selector it
        // does not know which pipeline definition to run.
        var target = new Dictionary<string, object>
        {
            ["type"] = "pipeline_ref_target",
            ["ref_type"] = "branch",
            ["ref_name"] = request.Branch,
            ["selector"] = string.IsNullOrEmpty(request.Pattern)
                ? new { type = "branches", pattern = "default" }
                : new { type = "custom", pattern = request.Pattern },
        };

        var body = new
        {
            type = "pipeline_schedule",
            enabled = request.Enabled,
            cron_pattern = request.Cron,
            target,
        };

        var schedule = await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines_config/schedules/", body, ct);

        return new
        {
            message = "Schedule created successfully",
            schedule = new
            {
                uuid = PipelineFormat.GetString(schedule, "uuid"),
                cron_pattern = request.Cron,
                enabled = request.Enabled,
                branch = request.Branch,
            },
        };
    }
}
