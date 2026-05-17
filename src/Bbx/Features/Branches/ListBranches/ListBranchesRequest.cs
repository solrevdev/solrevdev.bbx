namespace Bbx.Features.Branches.ListBranches;

public sealed record ListBranchesRequest(
    string? Workspace,
    string? Repository,
    int Limit,
    string? Sort,
    string? Query);
