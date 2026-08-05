namespace Bbx.Features.PullRequests.PullRequestDiffstat;

public sealed record PullRequestDiffstatRequest(string? Workspace, string? Repository, int Id, int Limit);
