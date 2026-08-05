using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewPipelineSchedule;

public sealed class ViewPipelineScheduleHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewPipelineScheduleRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/schedules/{Uri.EscapeDataString(request.ScheduleUuid)}", ct);
    }
}
