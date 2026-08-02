namespace Bbx.Features.Pipelines.ListTestReports;

public sealed record ListTestReportsRequest(
    string? Workspace,
    string? Repository,
    string PipelineUuid,
    string StepUuid);
