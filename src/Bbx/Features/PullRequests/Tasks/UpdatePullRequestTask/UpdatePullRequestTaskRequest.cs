namespace Bbx.Features.PullRequests.Tasks.UpdatePullRequestTask;

public sealed record UpdatePullRequestTaskRequest(
    string? Workspace,
    string? Repository,
    int Id,
    int TaskId,
    string? Content,
    string? State);
