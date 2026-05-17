namespace Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;

public sealed record ListRepoDeployKeysRequest(string? Workspace, string? Repository, int Limit);
