namespace Bbx.Features.Commits.ListCommitPullRequests;

public sealed record ListCommitPullRequestsRequest(string? Workspace, string? Repository, string Hash);
