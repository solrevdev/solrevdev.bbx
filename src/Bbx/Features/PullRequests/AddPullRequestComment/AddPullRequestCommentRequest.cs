namespace Bbx.Features.PullRequests.AddPullRequestComment;

public sealed record AddPullRequestCommentRequest(string? Workspace, string? Repository, int Id, string Body);
