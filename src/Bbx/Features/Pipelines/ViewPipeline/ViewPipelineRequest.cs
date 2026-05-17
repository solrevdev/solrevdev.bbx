namespace Bbx.Features.Pipelines.ViewPipeline;

public sealed record ViewPipelineRequest(string? Workspace, string? Repository, string PipelineUuid);
