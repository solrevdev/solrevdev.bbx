using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.UpsertReportAnnotation;

public sealed class UpsertReportAnnotationHandler(BitbucketClient client, CredentialManager credentials)
{
    public static readonly string[] Types = ["VULNERABILITY", "CODE_SMELL", "BUG"];
    public static readonly string[] Severities = ["HIGH", "MEDIUM", "LOW", "CRITICAL"];

    public const string LineNeedsPath = "Error: --line needs --path to say which file the line is in.";

    public async Task<JsonElement> HandleAsync(UpsertReportAnnotationRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (request.AnnotationType is not null && !Types.Contains(request.AnnotationType, StringComparer.OrdinalIgnoreCase))
            throw new BbxUserException($"Error: --type must be one of {string.Join(", ", Types)}.");
        if (request.Severity is not null && !Severities.Contains(request.Severity, StringComparer.OrdinalIgnoreCase))
            throw new BbxUserException($"Error: --severity must be one of {string.Join(", ", Severities)}.");
        if (request.Line is not null && string.IsNullOrEmpty(request.Path))
            throw new BbxUserException(LineNeedsPath);

        var report = $"repositories/{ws}/{repo}/commit/{request.Hash}"
            + $"/reports/{Uri.EscapeDataString(request.ReportId)}";

        var body = new Dictionary<string, object>
        {
            ["type"] = "report_annotation",
            ["summary"] = request.Summary,
        };
        if (request.Details is not null) body["details"] = request.Details;
        if (request.AnnotationType is not null) body["annotation_type"] = request.AnnotationType.ToUpperInvariant();
        if (request.Severity is not null) body["severity"] = request.Severity.ToUpperInvariant();
        if (!string.IsNullOrEmpty(request.Path)) body["path"] = request.Path;
        if (request.Line is not null) body["line"] = request.Line.Value;

        return await client.PutAsync<JsonElement>(
            $"{report}/annotations/{Uri.EscapeDataString(request.AnnotationId)}", body, ct);
    }
}
