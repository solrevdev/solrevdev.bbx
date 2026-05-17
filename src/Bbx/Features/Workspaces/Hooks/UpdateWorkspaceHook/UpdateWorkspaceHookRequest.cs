namespace Bbx.Features.Workspaces.Hooks.UpdateWorkspaceHook;

public sealed record UpdateWorkspaceHookRequest(
    string? Workspace,
    string Uid,
    string? Url,
    string? Description,
    string[]? Events,
    bool? Active);
