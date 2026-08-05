namespace Bbx.Features.Workspaces.Projects.Access.RemoveProjectGroupPermission;

public sealed record RemoveProjectGroupPermissionRequest(
    string? Workspace,
    string ProjectKey,
    string GroupSlug);
