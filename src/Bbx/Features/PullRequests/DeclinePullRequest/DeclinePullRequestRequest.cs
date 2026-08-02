namespace Bbx.Features.PullRequests.DeclinePullRequest;

public sealed record DeclinePullRequestRequest(string? Workspace, string? Repository, int Id, string? Reason);
