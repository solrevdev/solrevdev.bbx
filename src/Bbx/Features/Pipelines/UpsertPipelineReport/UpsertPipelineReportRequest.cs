namespace Bbx.Features.Pipelines.UpsertPipelineReport;

public sealed record UpsertPipelineReportRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId,
    string Title,
    string? Details,
    string? ReportType,
    string? Result,
    string? Link);
