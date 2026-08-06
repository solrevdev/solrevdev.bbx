namespace Bbx.Features.Branches.UpdateBranchRestriction;

public sealed record UpdateBranchRestrictionRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string? Pattern,
    int? Value,
    string[]? Users,
    string[]? Groups);
