namespace Bbx.Features.Pipelines.ViewDeploymentEnvironment;

public sealed record ViewDeploymentEnvironmentRequest(string? Workspace, string? Repository, string Environment);
