namespace Bbx.Features.PullRequests.PullRequestMergeStatus;

/// <param name="TaskId">The task ID, or the whole task-status URL.</param>
/// <param name="PollInterval">Null for the default. Tests pass zero.</param>
public sealed record PullRequestMergeStatusRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string TaskId,
    bool Wait = false,
    int TimeoutSeconds = MergeTask.DefaultTimeoutSeconds,
    TimeSpan? PollInterval = null);
