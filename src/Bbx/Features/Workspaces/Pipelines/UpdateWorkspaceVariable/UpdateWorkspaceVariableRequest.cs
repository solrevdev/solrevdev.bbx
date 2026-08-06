namespace Bbx.Features.Workspaces.Pipelines.UpdateWorkspaceVariable;

public sealed record UpdateWorkspaceVariableRequest(
    string? Workspace,
    string VariableUuid,
    string? Key,
    string? Value,
    bool? Secured);
