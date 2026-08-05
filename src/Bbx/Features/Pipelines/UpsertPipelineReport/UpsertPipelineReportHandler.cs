using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.UpsertPipelineReport;

public sealed class UpsertPipelineReportHandler(BitbucketClient client, CredentialManager credentials)
{
    public static readonly string[] Types = ["SECURITY", "COVERAGE", "TEST", "BUG"];
    public static readonly string[] Results = ["PASSED", "FAILED", "PENDING"];

    public async Task<object> HandleAsync(UpsertPipelineReportRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (request.ReportType is not null && !Types.Contains(request.ReportType, StringComparer.OrdinalIgnoreCase))
            throw new BbxUserException($"Error: --type must be one of {string.Join(", ", Types)}.");
        if (request.Result is not null && !Results.Contains(request.Result, StringComparer.OrdinalIgnoreCase))
            throw new BbxUserException($"Error: --result must be one of {string.Join(", ", Results)}.");

        var report = $"repositories/{ws}/{repo}/commit/{request.Hash}"
            + $"/reports/{Uri.EscapeDataString(request.ReportId)}";

        // The report ID is chosen by the caller, so this PUT creates as well as
        // updates. That is what makes it usable from a CI step that has no
        // record of whether it has run before.
        var body = new Dictionary<string, object>
        {
            ["title"] = request.Title,
            ["report_type"] = (request.ReportType ?? "TEST").ToUpperInvariant(),
        };
        if (request.Details is not null) body["details"] = request.Details;
        if (request.Result is not null) body["result"] = request.Result.ToUpperInvariant();
        if (request.Link is not null) body["link"] = request.Link;

        var created = await client.PutAsync<JsonElement>(report, body, ct);
        return ListPipelineReports.ListPipelineReportsHandler.ProjectReport(created);
    }
}
