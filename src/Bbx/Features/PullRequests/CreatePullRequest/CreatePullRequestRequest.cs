namespace Bbx.Features.PullRequests.CreatePullRequest;

public sealed record CreatePullRequestRequest(
    string? Workspace,
    string? Repository,
    string Title,
    string Source,
    string Destination,
    string? Body,
    string[]? Reviewers,
    bool CloseSourceBranch);
