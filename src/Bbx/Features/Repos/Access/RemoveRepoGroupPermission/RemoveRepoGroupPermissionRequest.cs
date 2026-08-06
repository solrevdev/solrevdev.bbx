namespace Bbx.Features.Repos.Access.RemoveRepoGroupPermission;

public sealed record RemoveRepoGroupPermissionRequest(
    string? Workspace,
    string? Repository,
    string GroupSlug);
