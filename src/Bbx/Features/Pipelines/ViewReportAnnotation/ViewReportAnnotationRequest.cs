namespace Bbx.Features.Pipelines.ViewReportAnnotation;

public sealed record ViewReportAnnotationRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId,
    string AnnotationId);
