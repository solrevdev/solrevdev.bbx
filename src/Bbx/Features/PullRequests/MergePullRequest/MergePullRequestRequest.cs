using Bbx.Features.PullRequests.PullRequestMergeStatus;

namespace Bbx.Features.PullRequests.MergePullRequest;

/// <param name="Wait">Poll a merge Bitbucket queued (202) until it finishes or the timeout passes.</param>
/// <param name="PollInterval">Null for the default. Tests pass zero.</param>
public sealed record MergePullRequestRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string? Strategy,
    string? Message,
    bool CloseSourceBranch,
    bool Wait = true,
    int TimeoutSeconds = MergeTask.DefaultTimeoutSeconds,
    TimeSpan? PollInterval = null);
