namespace Bbx.Features.Commits.Comments.AddCommitComment;

public sealed record AddCommitCommentRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string Body,
    string? Path,
    int? Line);
