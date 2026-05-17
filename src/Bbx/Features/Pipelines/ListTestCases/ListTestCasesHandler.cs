using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListTestCases;

public sealed class ListTestCasesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListTestCasesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint =
            $"/repositories/{ws}/{repo}/pipelines/{Uri.EscapeDataString(request.PipelineUuid)}/steps/{Uri.EscapeDataString(request.StepUuid)}/test_reports/test_cases";

        var cases = new List<object>();
        var count = 0;
        await foreach (var c in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            cases.Add(new
            {
                name = c.TryGetProperty("name", out var n) ? n.GetString() : null,
                classname = c.TryGetProperty("classname", out var cl) ? cl.GetString() : null,
                status = c.TryGetProperty("status", out var s) ? s.GetString() : null,
                duration = c.TryGetProperty("duration_in_ms", out var d) && d.ValueKind == JsonValueKind.Number
                    ? d.GetInt32()
                    : (int?)null,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            pipeline_uuid = request.PipelineUuid,
            step_uuid = request.StepUuid,
            count = cases.Count,
            test_cases = cases,
        };
    }
}
