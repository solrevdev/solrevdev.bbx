using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListTestReports;

public sealed class ListTestReportsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ListTestReportsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        // pipelines/{uuid}/steps/{step-uuid}/test_reports — Bitbucket's
        // test report endpoint nests under the step, not the pipeline.
        return await client.GetAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pipelines/{Uri.EscapeDataString(request.PipelineUuid)}/steps/{Uri.EscapeDataString(request.StepUuid)}/test_reports",
            ct);
    }
}
