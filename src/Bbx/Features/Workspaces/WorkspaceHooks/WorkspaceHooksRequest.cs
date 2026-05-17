namespace Bbx.Features.Workspaces.WorkspaceHooks;

public sealed record WorkspaceHooksRequest(
    string? Workspace,
    string? View,
    string? Create,
    string? Description,
    string[]? Events,
    bool? Active,
    string? DeleteUuid,
    int Limit);
