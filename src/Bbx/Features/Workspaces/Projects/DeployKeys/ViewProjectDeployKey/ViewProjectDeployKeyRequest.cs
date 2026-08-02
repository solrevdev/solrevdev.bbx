namespace Bbx.Features.Workspaces.Projects.DeployKeys.ViewProjectDeployKey;

public sealed record ViewProjectDeployKeyRequest(string? Workspace, string ProjectKey, int KeyId);
