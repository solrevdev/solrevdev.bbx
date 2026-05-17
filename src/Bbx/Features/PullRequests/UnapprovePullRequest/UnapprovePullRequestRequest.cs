namespace Bbx.Features.PullRequests.UnapprovePullRequest;

public sealed record UnapprovePullRequestRequest(string? Workspace, string? Repository, int Id);
