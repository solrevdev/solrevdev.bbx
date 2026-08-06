namespace Bbx.Features.Workspaces.ListWorkspaceRepositoryPermissions;

/// <summary>
/// With <paramref name="Repository"/> set, only that repository's grants.
/// </summary>
public sealed record ListWorkspaceRepositoryPermissionsRequest(
    string? Workspace,
    string? Repository,
    int Limit);
