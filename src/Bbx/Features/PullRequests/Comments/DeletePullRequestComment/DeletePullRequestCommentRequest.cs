namespace Bbx.Features.PullRequests.Comments.DeletePullRequestComment;

public sealed record DeletePullRequestCommentRequest(string? Workspace, string? Repository, int Id, int CommentId);
