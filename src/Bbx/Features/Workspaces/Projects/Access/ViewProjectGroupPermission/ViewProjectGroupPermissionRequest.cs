namespace Bbx.Features.Workspaces.Projects.Access.ViewProjectGroupPermission;

public sealed record ViewProjectGroupPermissionRequest(
    string? Workspace,
    string ProjectKey,
    string GroupSlug);
