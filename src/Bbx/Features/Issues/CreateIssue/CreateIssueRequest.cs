namespace Bbx.Features.Issues.CreateIssue;

public sealed record CreateIssueRequest(
    string? Workspace,
    string? Repository,
    string Title,
    string? Content,
    string? Kind,
    string? Priority);
