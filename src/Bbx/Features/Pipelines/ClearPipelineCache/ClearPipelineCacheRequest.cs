namespace Bbx.Features.Pipelines.ClearPipelineCache;

public sealed record ClearPipelineCacheRequest(string? Workspace, string? Repository, string Name);
