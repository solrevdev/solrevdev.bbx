namespace Bbx.Features.Issues.DeleteIssue;

public sealed record DeleteIssueRequest(string? Workspace, string? Repository, int Id);
