namespace Bbx.Features.Issues.ListIssues;

public sealed record ListIssuesRequest(
    string? Workspace,
    string? Repository,
    string? State,
    string? Priority,
    string? Assignee,
    int Limit);
