namespace Bbx.Features.Repos.DeployKeys.ViewRepoDeployKey;

public sealed record ViewRepoDeployKeyRequest(string? Workspace, string? Repository, int KeyId);
