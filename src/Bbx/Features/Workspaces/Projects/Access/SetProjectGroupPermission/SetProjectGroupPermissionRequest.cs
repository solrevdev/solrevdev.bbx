namespace Bbx.Features.Workspaces.Projects.Access.SetProjectGroupPermission;

public sealed record SetProjectGroupPermissionRequest(
    string? Workspace,
    string ProjectKey,
    string GroupSlug,
    string Permission);
