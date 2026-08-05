namespace Bbx.Features.Pipelines.ListDeploymentVariables;

public sealed record ListDeploymentVariablesRequest(
    string? Workspace,
    string? Repository,
    string Environment,
    int Limit);
