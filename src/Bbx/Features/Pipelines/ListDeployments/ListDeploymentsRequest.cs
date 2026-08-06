namespace Bbx.Features.Pipelines.ListDeployments;

public sealed record ListDeploymentsRequest(
    string? Workspace,
    string? Repository,
    int Limit);
