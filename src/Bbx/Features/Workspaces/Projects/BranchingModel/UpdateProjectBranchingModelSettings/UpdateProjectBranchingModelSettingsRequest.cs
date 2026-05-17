namespace Bbx.Features.Workspaces.Projects.BranchingModel.UpdateProjectBranchingModelSettings;

public sealed record UpdateProjectBranchingModelSettingsRequest(
    string? Workspace,
    string ProjectKey,
    string SettingsJson);
