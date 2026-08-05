namespace Bbx.Features.Repos.Access.ViewRepoUserPermission;

public sealed record ViewRepoUserPermissionRequest(
    string? Workspace,
    string? Repository,
    string AccountId);
