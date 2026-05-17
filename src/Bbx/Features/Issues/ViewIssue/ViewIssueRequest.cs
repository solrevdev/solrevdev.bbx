namespace Bbx.Features.Issues.ViewIssue;

public sealed record ViewIssueRequest(string? Workspace, string? Repository, int Id);
