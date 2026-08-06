namespace Bbx.Features.Repos.OverrideSettings.UpdateOverrideSettings;

public sealed record UpdateOverrideSettingsRequest(
    string? Workspace,
    string? Repository,
    bool? DefaultReviewers,
    bool? BranchingModel,
    bool? BranchRestrictions);
