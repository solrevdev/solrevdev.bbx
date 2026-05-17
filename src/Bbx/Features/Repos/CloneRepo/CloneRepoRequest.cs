namespace Bbx.Features.Repos.CloneRepo;

public sealed record CloneRepoRequest(string? Workspace, string Repository, bool Ssh);
