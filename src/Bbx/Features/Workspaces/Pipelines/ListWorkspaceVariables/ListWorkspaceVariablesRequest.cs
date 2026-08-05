namespace Bbx.Features.Workspaces.Pipelines.ListWorkspaceVariables;

public sealed record ListWorkspaceVariablesRequest(
    string? Workspace,
    int Limit);
