namespace Bbx.Features.Branches.AddBranchRestriction;

public sealed record AddBranchRestrictionRequest(string? Workspace, string? Repository, string Kind, string Pattern);
