namespace Bbx.Features.PullRequests.PullRequestActivity;

/// <summary>
/// <paramref name="Id"/> is null for the repository-wide activity feed, which
/// is a different endpoint from one pull request's own log.
/// </summary>
public sealed record PullRequestActivityRequest(string? Workspace, string? Repository, int? Id, int Limit);
