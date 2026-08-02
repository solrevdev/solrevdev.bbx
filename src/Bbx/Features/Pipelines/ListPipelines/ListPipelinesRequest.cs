namespace Bbx.Features.Pipelines.ListPipelines;

public sealed record ListPipelinesRequest(
    string? Workspace,
    string? Repository,
    string? Status,
    string Sort,
    int Limit,
    string? Branch);
