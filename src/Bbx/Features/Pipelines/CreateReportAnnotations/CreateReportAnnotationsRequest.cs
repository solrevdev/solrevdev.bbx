namespace Bbx.Features.Pipelines.CreateReportAnnotations;

public sealed record CreateReportAnnotationsRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId,
    string AnnotationsJson);
