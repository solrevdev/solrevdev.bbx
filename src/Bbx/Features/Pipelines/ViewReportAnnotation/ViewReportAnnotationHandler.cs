using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewReportAnnotation;

public sealed class ViewReportAnnotationHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewReportAnnotationRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var report = $"repositories/{ws}/{repo}/commit/{request.Hash}"
            + $"/reports/{Uri.EscapeDataString(request.ReportId)}";
        return await client.GetAsync<JsonElement>(
            $"{report}/annotations/{Uri.EscapeDataString(request.AnnotationId)}", ct);
    }
}
