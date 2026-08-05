namespace Bbx.Features.PullRequests.Comments.ViewPullRequestComment;

public sealed record ViewPullRequestCommentRequest(string? Workspace, string? Repository, int Id, int CommentId);
