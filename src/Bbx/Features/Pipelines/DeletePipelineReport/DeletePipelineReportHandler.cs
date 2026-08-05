using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeletePipelineReport;

public sealed class DeletePipelineReportHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeletePipelineReportRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var report = $"repositories/{ws}/{repo}/commit/{request.Hash}"
            + $"/reports/{Uri.EscapeDataString(request.ReportId)}";
        await client.DeleteAsync(report, ct);
        return $"\u2713 Deleted report {request.ReportId} on commit {request.Hash}";
    }
}
