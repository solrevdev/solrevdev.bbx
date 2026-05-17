namespace Bbx.Features.PullRequests.ListPullRequests;

public sealed record ListPullRequestsRequest(
    string? Workspace,
    string? Repository,
    string? State,
    string? Author,
    int Limit);
