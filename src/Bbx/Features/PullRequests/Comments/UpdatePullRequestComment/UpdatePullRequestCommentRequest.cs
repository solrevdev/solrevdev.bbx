namespace Bbx.Features.PullRequests.Comments.UpdatePullRequestComment;

public sealed record UpdatePullRequestCommentRequest(
    string? Workspace,
    string? Repository,
    int Id,
    int CommentId,
    string Body);
