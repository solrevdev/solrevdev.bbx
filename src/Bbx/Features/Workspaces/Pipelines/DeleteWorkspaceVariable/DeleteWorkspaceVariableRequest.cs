namespace Bbx.Features.Workspaces.Pipelines.DeleteWorkspaceVariable;

public sealed record DeleteWorkspaceVariableRequest(
    string? Workspace,
    string VariableUuid);
