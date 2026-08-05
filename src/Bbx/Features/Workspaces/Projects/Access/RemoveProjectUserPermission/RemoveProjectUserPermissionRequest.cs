namespace Bbx.Features.Workspaces.Projects.Access.RemoveProjectUserPermission;

public sealed record RemoveProjectUserPermissionRequest(
    string? Workspace,
    string ProjectKey,
    string AccountId);
