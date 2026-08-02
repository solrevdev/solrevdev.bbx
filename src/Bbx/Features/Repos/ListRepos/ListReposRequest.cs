namespace Bbx.Features.Repos.ListRepos;

public sealed record ListReposRequest(string? Workspace, int Limit, string? Query);
