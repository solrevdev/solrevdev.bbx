using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.UpdatePipelineSchedule;

public sealed class UpdatePipelineScheduleHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass --enabled, --disabled or --cron.";

    public async Task<JsonElement> HandleAsync(UpdatePipelineScheduleRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var body = new Dictionary<string, object>();
        if (request.Enabled is not null) body["enabled"] = request.Enabled.Value;
        if (request.Cron is not null) body["cron_pattern"] = request.Cron;
        if (body.Count == 0) throw new BbxUserException(NothingToUpdate);

        return await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/schedules/{Uri.EscapeDataString(request.ScheduleUuid)}",
            body, ct);
    }
}
