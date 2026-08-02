namespace Bbx.Features.Repos.Hooks.DeleteRepoHook;

public sealed record DeleteRepoHookRequest(string? Workspace, string? Repository, string Uid);
