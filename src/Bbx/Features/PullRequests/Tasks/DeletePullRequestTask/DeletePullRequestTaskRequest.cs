namespace Bbx.Features.PullRequests.Tasks.DeletePullRequestTask;

public sealed record DeletePullRequestTaskRequest(string? Workspace, string? Repository, int Id, int TaskId);
