namespace Bbx.Features.Repos.Hooks.CreateRepoHook;

public sealed record CreateRepoHookRequest(
    string? Workspace,
    string? Repository,
    string Url,
    string? Description,
    string[]? Events,
    bool Active);
