namespace Bbx.Features.Branches.DeleteBranch;

public sealed record DeleteBranchRequest(string? Workspace, string? Repository, string Name);
