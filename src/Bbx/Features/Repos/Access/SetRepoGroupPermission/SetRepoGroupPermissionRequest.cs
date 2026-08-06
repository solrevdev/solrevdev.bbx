namespace Bbx.Features.Repos.Access.SetRepoGroupPermission;

public sealed record SetRepoGroupPermissionRequest(
    string? Workspace,
    string? Repository,
    string GroupSlug,
    string Permission);
