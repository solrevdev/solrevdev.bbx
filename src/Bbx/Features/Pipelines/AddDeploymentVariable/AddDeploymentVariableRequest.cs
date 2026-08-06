namespace Bbx.Features.Pipelines.AddDeploymentVariable;

public sealed record AddDeploymentVariableRequest(
    string? Workspace,
    string? Repository,
    string Environment,
    string Key,
    string Value,
    bool Secured);
