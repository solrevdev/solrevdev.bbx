namespace Bbx.Features.Repos.Access.ListRepoGroupPermissions;

public sealed record ListRepoGroupPermissionsRequest(
    string? Workspace,
    string? Repository,
    int Limit);
