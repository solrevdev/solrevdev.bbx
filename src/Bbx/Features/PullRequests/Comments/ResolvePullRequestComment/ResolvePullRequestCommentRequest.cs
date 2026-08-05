namespace Bbx.Features.PullRequests.Comments.ResolvePullRequestComment;

/// <summary>
/// Resolving and reopening are the same resource under two verbs, so one
/// request carries both.
/// </summary>
public sealed record ResolvePullRequestCommentRequest(
    string? Workspace,
    string? Repository,
    int Id,
    int CommentId,
    bool Resolve);
