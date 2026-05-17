namespace Bbx.Features.Workspaces.Projects.DeployKeys.DeleteProjectDeployKey;

public sealed record DeleteProjectDeployKeyRequest(string? Workspace, string ProjectKey, int KeyId);
