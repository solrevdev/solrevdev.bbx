namespace Bbx.Features.Pipelines.ViewDeployment;

public sealed record ViewDeploymentRequest(
    string? Workspace,
    string? Repository,
    string DeploymentUuid);
