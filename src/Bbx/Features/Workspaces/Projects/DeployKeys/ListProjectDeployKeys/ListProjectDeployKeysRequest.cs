namespace Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;

public sealed record ListProjectDeployKeysRequest(string? Workspace, string ProjectKey, int Limit);
