namespace Bbx.Features.Repos.DeployKeys.AddRepoDeployKey;

public sealed record AddRepoDeployKeyRequest(
    string? Workspace,
    string? Repository,
    string Key,
    string? Label);
