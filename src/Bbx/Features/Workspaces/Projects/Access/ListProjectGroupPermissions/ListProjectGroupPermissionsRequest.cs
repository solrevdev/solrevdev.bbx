namespace Bbx.Features.Workspaces.Projects.Access.ListProjectGroupPermissions;

public sealed record ListProjectGroupPermissionsRequest(
    string? Workspace,
    string ProjectKey,
    int Limit);
