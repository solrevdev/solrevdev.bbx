namespace Bbx.Features.Repos.BranchingModel.UpdateBranchingModelSettings;

public sealed record UpdateBranchingModelSettingsRequest(
    string? Workspace,
    string? Repository,
    string SettingsJson);
