namespace Bbx.Features.Pipelines.DeleteReportAnnotation;

public sealed record DeleteReportAnnotationRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId,
    string AnnotationId);
