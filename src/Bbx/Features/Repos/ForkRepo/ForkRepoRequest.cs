namespace Bbx.Features.Repos.ForkRepo;

public sealed record ForkRepoRequest(string? Workspace, string Repository, string? Name, string? ToWorkspace);
