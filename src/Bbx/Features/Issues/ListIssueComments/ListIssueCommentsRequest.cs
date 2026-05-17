namespace Bbx.Features.Issues.ListIssueComments;

public sealed record ListIssueCommentsRequest(string? Workspace, string? Repository, int Id);
