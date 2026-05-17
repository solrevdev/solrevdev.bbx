namespace Bbx.Features.Pipelines.ListPipelineReports;

public sealed record ListPipelineReportsRequest(string? Workspace, string? Repository, string Hash, int Limit);
