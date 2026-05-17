namespace Bbx.Features.Pipelines.ViewPipelineReport;

public sealed record ViewPipelineReportRequest(string? Workspace, string? Repository, string Hash, string ReportId);
