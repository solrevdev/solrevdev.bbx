namespace Bbx.Features.Pipelines.UpsertReportAnnotation;

public sealed record UpsertReportAnnotationRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId,
    string AnnotationId,
    string Summary,
    string? Details,
    string? AnnotationType,
    string? Severity,
    string? Path,
    int? Line);
