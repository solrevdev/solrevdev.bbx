namespace Bbx.Features.Pipelines.DeletePipelineReport;

public sealed record DeletePipelineReportRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string ReportId);
