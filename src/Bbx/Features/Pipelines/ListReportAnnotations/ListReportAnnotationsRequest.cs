namespace Bbx.Features.Pipelines.ListReportAnnotations;

public sealed record ListReportAnnotationsRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId,
    int Limit);
