namespace Bbx.Features.Workspaces.Projects.Access.ViewProjectUserPermission;

public sealed record ViewProjectUserPermissionRequest(
    string? Workspace,
    string ProjectKey,
    string AccountId);
