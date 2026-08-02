namespace Bbx.Features.Repos.Hooks.ViewRepoHook;

public sealed record ViewRepoHookRequest(string? Workspace, string? Repository, string Uid);
