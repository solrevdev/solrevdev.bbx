namespace Bbx.Features.Pipelines.DeleteDeploymentEnvironment;

public sealed record DeleteDeploymentEnvironmentRequest(
    string? Workspace,
    string? Repository,
    string Environment);
