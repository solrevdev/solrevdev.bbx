namespace Bbx.Features.Repos.DeployKeys.UpdateRepoDeployKey;

public sealed record UpdateRepoDeployKeyRequest(
    string? Workspace,
    string? Repository,
    int KeyId,
    string Key,
    string? Label);
