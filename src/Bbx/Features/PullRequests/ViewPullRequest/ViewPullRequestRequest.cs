namespace Bbx.Features.PullRequests.ViewPullRequest;

public sealed record ViewPullRequestRequest(string? Workspace, string? Repository, int Id);
