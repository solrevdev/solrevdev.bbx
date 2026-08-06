namespace Bbx.Features.Pipelines.ChangeDeploymentEnvironment;

public sealed record ChangeDeploymentEnvironmentRequest(
    string? Workspace,
    string? Repository,
    string Environment,
    string? Name,
    bool? AdminOnly);
