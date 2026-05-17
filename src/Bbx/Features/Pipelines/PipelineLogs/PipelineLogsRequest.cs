namespace Bbx.Features.Pipelines.PipelineLogs;

public sealed record PipelineLogsRequest(
    string? Workspace,
    string? Repository,
    string PipelineUuid,
    string StepUuid);
