namespace Bbx.Features.Commits.Comments.DeleteCommitComment;

public sealed record DeleteCommitCommentRequest(string? Workspace, string? Repository, string Hash, int CommentId);
