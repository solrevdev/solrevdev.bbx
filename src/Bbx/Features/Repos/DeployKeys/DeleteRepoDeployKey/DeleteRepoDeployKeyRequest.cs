namespace Bbx.Features.Repos.DeployKeys.DeleteRepoDeployKey;

public sealed record DeleteRepoDeployKeyRequest(string? Workspace, string? Repository, int KeyId);
