namespace Bbx.Features.PullRequests.ListPullRequestCommits;

public sealed record ListPullRequestCommitsRequest(string? Workspace, string? Repository, int Id, int Limit);
