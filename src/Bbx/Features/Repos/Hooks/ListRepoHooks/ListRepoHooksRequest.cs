namespace Bbx.Features.Repos.Hooks.ListRepoHooks;

public sealed record ListRepoHooksRequest(string? Workspace, string? Repository, int Limit);
