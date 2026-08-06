namespace Bbx.Features.PullRequests.Tasks.ViewPullRequestTask;

public sealed record ViewPullRequestTaskRequest(string? Workspace, string? Repository, int Id, int TaskId);
