namespace Bbx.Features.Pipelines.ListPipelineKnownHosts;

public sealed record ListPipelineKnownHostsRequest(
    string? Workspace,
    string? Repository,
    int Limit);
