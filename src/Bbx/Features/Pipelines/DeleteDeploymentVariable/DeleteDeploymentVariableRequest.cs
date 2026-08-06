namespace Bbx.Features.Pipelines.DeleteDeploymentVariable;

public sealed record DeleteDeploymentVariableRequest(
    string? Workspace,
    string? Repository,
    string Environment,
    string VariableUuid);
