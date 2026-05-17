namespace Bbx.Features.PullRequests.PullRequestDiff;

public sealed record PullRequestDiffRequest(string? Workspace, string? Repository, int Id);
