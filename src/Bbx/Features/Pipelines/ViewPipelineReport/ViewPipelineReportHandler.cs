using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Pipelines.ListPipelineReports;

namespace Bbx.Features.Pipelines.ViewPipelineReport;

public sealed class ViewPipelineReportHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewPipelineReportRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var report = await client.GetAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/reports/{Uri.EscapeDataString(request.ReportId)}", ct);
        return ListPipelineReportsHandler.ProjectReport(report);
    }
}
