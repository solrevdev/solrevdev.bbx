namespace Bbx.Features.Commits.Comments.UpdateCommitComment;

public sealed record UpdateCommitCommentRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    int CommentId,
    string Body);
