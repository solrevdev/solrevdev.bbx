using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineReports;

public sealed class ListPipelineReportsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineReportsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var reports = new List<object>();
        var count = 0;
        await foreach (var report in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/reports", ct))
        {
            reports.Add(ProjectReport(report));
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            repository = repo,
            commit = request.Hash,
            count = reports.Count,
            reports,
        };
    }

    internal static object ProjectReport(JsonElement r) => new
    {
        uuid = r.TryGetProperty("uuid", out var u) ? u.GetString() : null,
        external_id = r.TryGetProperty("external_id", out var e) ? e.GetString() : null,
        title = r.TryGetProperty("title", out var t) ? t.GetString() : null,
        report_type = r.TryGetProperty("report_type", out var rt) ? rt.GetString() : null,
        result = r.TryGetProperty("result", out var res) ? res.GetString() : null,
        reporter = r.TryGetProperty("reporter", out var rep) ? rep.GetString() : null,
        link = r.TryGetProperty("link", out var l) ? l.GetString() : null,
        created_on = r.TryGetProperty("created_on", out var c) ? c.GetString() : null,
    };
}
