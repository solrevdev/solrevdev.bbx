namespace Bbx.Features.Workspaces.Projects.Access.ListProjectUserPermissions;

public sealed record ListProjectUserPermissionsRequest(
    string? Workspace,
    string ProjectKey,
    int Limit);
