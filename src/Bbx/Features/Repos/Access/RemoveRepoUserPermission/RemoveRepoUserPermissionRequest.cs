namespace Bbx.Features.Repos.Access.RemoveRepoUserPermission;

public sealed record RemoveRepoUserPermissionRequest(
    string? Workspace,
    string? Repository,
    string AccountId);
