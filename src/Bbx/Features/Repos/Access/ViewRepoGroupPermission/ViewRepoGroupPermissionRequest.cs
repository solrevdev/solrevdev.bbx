namespace Bbx.Features.Repos.Access.ViewRepoGroupPermission;

public sealed record ViewRepoGroupPermissionRequest(
    string? Workspace,
    string? Repository,
    string GroupSlug);
