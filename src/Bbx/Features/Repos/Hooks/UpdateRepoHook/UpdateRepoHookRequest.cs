namespace Bbx.Features.Repos.Hooks.UpdateRepoHook;

public sealed record UpdateRepoHookRequest(
    string? Workspace,
    string? Repository,
    string Uid,
    string? Url,
    string? Description,
    string[]? Events,
    bool? Active);
