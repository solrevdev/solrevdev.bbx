using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListReportAnnotations;

public sealed class ListReportAnnotationsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListReportAnnotationsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint =
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/reports/{Uri.EscapeDataString(request.ReportId)}/annotations";

        var annotations = new List<object>();
        var count = 0;
        await foreach (var a in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            annotations.Add(new
            {
                uuid = a.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                external_id = a.TryGetProperty("external_id", out var e) ? e.GetString() : null,
                summary = a.TryGetProperty("summary", out var s) ? s.GetString() : null,
                annotation_type = a.TryGetProperty("annotation_type", out var at) ? at.GetString() : null,
                severity = a.TryGetProperty("severity", out var sev) ? sev.GetString() : null,
                path = a.TryGetProperty("path", out var p) ? p.GetString() : null,
                line = a.TryGetProperty("line", out var ln) && ln.ValueKind == JsonValueKind.Number
                    ? ln.GetInt32()
                    : (int?)null,
                result = a.TryGetProperty("result", out var r) ? r.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            commit = request.Hash,
            report_id = request.ReportId,
            count = annotations.Count,
            annotations,
        };
    }
}
