namespace Bbx.Features.Pipelines.StopPipeline;

public sealed record StopPipelineRequest(string? Workspace, string? Repository, string PipelineUuid);
