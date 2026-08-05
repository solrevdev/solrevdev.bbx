using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListTestCaseReasons;

public sealed class ListTestCaseReasonsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListTestCaseReasonsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // Why a test case failed: the stack trace and the assertion message.
        var endpoint = $"repositories/{ws}/{repo}/pipelines/{Uri.EscapeDataString(request.PipelineUuid)}"
            + $"/steps/{Uri.EscapeDataString(request.StepUuid)}/test_reports/test_cases"
            + $"/{Uri.EscapeDataString(request.TestCaseUuid)}/test_case_reasons";

        var reasons = new List<JsonElement>();
        var count = 0;
        await foreach (var reason in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            reasons.Add(reason);
            if (++count >= request.Limit) break;
        }

        return new { test_case_uuid = request.TestCaseUuid, count = reasons.Count, reasons };
    }
}
