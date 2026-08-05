namespace Bbx.Features.Pipelines.CreateDeploymentEnvironment;

public sealed record CreateDeploymentEnvironmentRequest(
    string? Workspace,
    string? Repository,
    string Name,
    string EnvironmentType,
    int Rank);
