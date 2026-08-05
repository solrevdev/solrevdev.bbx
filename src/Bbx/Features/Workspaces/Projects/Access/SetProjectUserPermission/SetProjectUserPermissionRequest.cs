namespace Bbx.Features.Workspaces.Projects.Access.SetProjectUserPermission;

public sealed record SetProjectUserPermissionRequest(
    string? Workspace,
    string ProjectKey,
    string AccountId,
    string Permission);
