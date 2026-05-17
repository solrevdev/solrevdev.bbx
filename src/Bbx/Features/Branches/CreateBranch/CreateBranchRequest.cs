namespace Bbx.Features.Branches.CreateBranch;

public sealed record CreateBranchRequest(string? Workspace, string? Repository, string Name, string Target);
