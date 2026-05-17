namespace Bbx.Features.PullRequests.PullRequestActivity;

public sealed record PullRequestActivityRequest(string? Workspace, string? Repository, int Id);
