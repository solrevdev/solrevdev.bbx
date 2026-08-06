using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeleteReportAnnotation;

public sealed class DeleteReportAnnotationHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteReportAnnotationRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var report = $"repositories/{ws}/{repo}/commit/{request.Hash}"
            + $"/reports/{Uri.EscapeDataString(request.ReportId)}";
        await client.DeleteAsync($"{report}/annotations/{Uri.EscapeDataString(request.AnnotationId)}", ct);
        return $"\u2713 Deleted annotation {request.AnnotationId}";
    }
}
