namespace Bbx.Features.PullRequests.ListPullRequestComments;

public sealed record ListPullRequestCommentsRequest(string? Workspace, string? Repository, int Id);
