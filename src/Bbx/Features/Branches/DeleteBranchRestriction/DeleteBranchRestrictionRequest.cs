namespace Bbx.Features.Branches.DeleteBranchRestriction;

public sealed record DeleteBranchRestrictionRequest(string? Workspace, string? Repository, int Id);
