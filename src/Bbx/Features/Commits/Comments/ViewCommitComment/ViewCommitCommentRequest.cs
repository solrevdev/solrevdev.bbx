namespace Bbx.Features.Commits.Comments.ViewCommitComment;

public sealed record ViewCommitCommentRequest(string? Workspace, string? Repository, string Hash, int CommentId);
