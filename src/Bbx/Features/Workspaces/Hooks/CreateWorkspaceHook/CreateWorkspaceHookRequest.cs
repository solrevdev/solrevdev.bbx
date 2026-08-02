namespace Bbx.Features.Workspaces.Hooks.CreateWorkspaceHook;

public sealed record CreateWorkspaceHookRequest(
    string? Workspace,
    string Url,
    string? Description,
    string[]? Events,
    bool Active);
