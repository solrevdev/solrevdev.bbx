using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineSchedules;

public sealed class ListPipelineSchedulesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineSchedulesRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var schedules = new List<JsonElement>();
        await foreach (var s in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines_config/schedules/", ct))
        {
            schedules.Add(s);
        }

        return new
        {
            workspace = ws,
            repository,
            count = schedules.Count,
            schedules = schedules.Select(s => new
            {
                uuid = PipelineFormat.GetString(s, "uuid"),
                enabled = s.TryGetProperty("enabled", out var en) && en.GetBoolean(),
                cron_pattern = PipelineFormat.GetString(s, "cron_pattern"),
                target = s.TryGetObject("target", out var t) ? (object)new
                {
                    ref_name = PipelineFormat.GetString(t, "ref_name"),
                    ref_type = PipelineFormat.GetString(t, "ref_type"),
                } : null!,
                created_on = PipelineFormat.GetString(s, "created_on"),
            }),
        };
    }
}
