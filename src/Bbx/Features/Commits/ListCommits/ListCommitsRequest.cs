namespace Bbx.Features.Commits.ListCommits;

public sealed record ListCommitsRequest(
    string? Workspace,
    string? Repository,
    string? Branch,
    string? Path,
    int Limit);
