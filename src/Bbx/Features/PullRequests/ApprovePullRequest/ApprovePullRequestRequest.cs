namespace Bbx.Features.PullRequests.ApprovePullRequest;

public sealed record ApprovePullRequestRequest(string? Workspace, string? Repository, int Id);
