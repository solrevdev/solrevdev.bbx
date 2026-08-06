namespace Bbx.Features.Pipelines.PipelineCacheContentUri;

public sealed record PipelineCacheContentUriRequest(
    string? Workspace,
    string? Repository,
    string CacheUuid);
