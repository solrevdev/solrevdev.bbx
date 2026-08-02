namespace Bbx.Features.Issues.AddIssueComment;

public sealed record AddIssueCommentRequest(string? Workspace, string? Repository, int Id, string Body);
