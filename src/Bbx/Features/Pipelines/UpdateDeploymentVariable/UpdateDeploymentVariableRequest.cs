namespace Bbx.Features.Pipelines.UpdateDeploymentVariable;

public sealed record UpdateDeploymentVariableRequest(
    string? Workspace,
    string? Repository,
    string Environment,
    string VariableUuid,
    string? Key,
    string? Value,
    bool? Secured);
