namespace Bbx.Features.Workspaces.Projects.DeployKeys.AddProjectDeployKey;

public sealed record AddProjectDeployKeyRequest(
    string? Workspace,
    string ProjectKey,
    string Key,
    string? Label);
