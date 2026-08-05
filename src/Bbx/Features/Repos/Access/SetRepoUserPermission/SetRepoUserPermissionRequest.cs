namespace Bbx.Features.Repos.Access.SetRepoUserPermission;

public sealed record SetRepoUserPermissionRequest(
    string? Workspace,
    string? Repository,
    string AccountId,
    string Permission);
