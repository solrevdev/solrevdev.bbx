namespace Bbx.Features.Issues.UpdateIssue;

public sealed record UpdateIssueRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string? Title,
    string? State,
    string? Priority,
    string? Assignee);
