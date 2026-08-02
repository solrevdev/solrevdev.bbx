namespace Bbx.Features.PullRequests.MergePullRequest;

public sealed record MergePullRequestRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string Strategy,
    string? Message,
    bool CloseSourceBranch);
