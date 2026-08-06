namespace Bbx.Features.Workspaces.Pipelines.AddWorkspaceVariable;

public sealed record AddWorkspaceVariableRequest(
    string? Workspace,
    string Key,
    string Value,
    bool Secured);
