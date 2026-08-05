namespace Bbx.Features.Pipelines.ClearAllPipelineCaches;

public sealed record ClearAllPipelineCachesRequest(
    string? Workspace,
    string? Repository);
