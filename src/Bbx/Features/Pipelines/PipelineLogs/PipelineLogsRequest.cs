namespace Bbx.Features.Pipelines.PipelineLogs;

/// <summary>
/// <paramref name="LogUuid"/> names one numbered log within the step. A step
/// that reran has one per attempt; without it the whole step's log comes back.
/// </summary>
public sealed record PipelineLogsRequest(
    string? Workspace,
    string? Repository,
    string PipelineUuid,
    string StepUuid,
    string? LogUuid = null);
