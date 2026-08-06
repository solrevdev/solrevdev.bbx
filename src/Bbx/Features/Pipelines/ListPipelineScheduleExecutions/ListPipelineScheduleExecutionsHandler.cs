using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineScheduleExecutions;

public sealed class ListPipelineScheduleExecutionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineScheduleExecutionsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var executions = new List<JsonElement>();
        var count = 0;
        await foreach (var execution in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/schedules/{Uri.EscapeDataString(request.ScheduleUuid)}/executions", ct))
        {
            executions.Add(execution);
            if (++count >= request.Limit) break;
        }

        return new { schedule_uuid = request.ScheduleUuid, count = executions.Count, executions };
    }
}
