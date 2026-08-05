namespace Bbx.Features.Workspaces.Pipelines.ViewWorkspaceVariable;

public sealed record ViewWorkspaceVariableRequest(
    string? Workspace,
    string VariableUuid);
