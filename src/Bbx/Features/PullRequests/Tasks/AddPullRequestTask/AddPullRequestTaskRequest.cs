namespace Bbx.Features.PullRequests.Tasks.AddPullRequestTask;

public sealed record AddPullRequestTaskRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string Content);
