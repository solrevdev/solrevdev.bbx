using System.Text.Json;
using System.Xml;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListTestCases;

public sealed class ListTestCasesHandler(BitbucketClient client, CredentialManager credentials)
{
    /// <summary>
    /// Printed beside an empty page. Bitbucket returns only failed cases, so an empty
    /// page nearly always means every test passed, and reads as "no tests ran" without this.
    /// </summary>
    internal const string OnlyFailedCasesNote =
        "Bitbucket lists only failed test cases. Passed and skipped cases are never returned, "
        + "so a step whose tests all passed answers an empty list. Run bbx pipeline test-reports for the counts.";

    public async Task<object> HandleAsync(ListTestCasesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint =
            $"/repositories/{ws}/{repo}/pipelines/{Uri.EscapeDataString(request.PipelineUuid)}/steps/{Uri.EscapeDataString(request.StepUuid)}/test_reports/test_cases";

        var cases = new List<object>();
        await foreach (var c in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            cases.Add(TestCase(c));
            if (cases.Count >= request.Limit) break;
        }

        return new
        {
            pipeline_uuid = request.PipelineUuid,
            step_uuid = request.StepUuid,
            count = cases.Count,
            test_cases = cases,
            note = cases.Count == 0 ? OnlyFailedCasesNote : null,
        };
    }

    // Field names read off a live failed case on 2026-09-12; the spec documents no schema.
    // This used to read "classname" and "duration_in_ms", neither of which exists, so both
    // printed null, and it dropped "uuid", which test-case-reasons needs.
    private static object TestCase(JsonElement c) => new
    {
        uuid = c.GetStringOrNull("uuid"),
        name = c.GetStringOrNull("name"),
        classname = c.GetStringOrNull("class_name"),
        fully_qualified_name = c.GetStringOrNull("fully_qualified_name"),
        status = c.GetStringOrNull("status"),
        duration_ms = DurationMs(c.GetStringOrNull("duration")),
        message = c.TryGetObject("reason", out var reason) ? reason.GetStringOrNull("message") : null,
    };

    /// <summary>Bitbucket sends an ISO 8601 duration such as <c>PT0.9589264S</c>.</summary>
    internal static long? DurationMs(string? iso)
    {
        if (string.IsNullOrEmpty(iso)) return null;
        try
        {
            return (long)Math.Round(XmlConvert.ToTimeSpan(iso).TotalMilliseconds);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
