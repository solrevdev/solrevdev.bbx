namespace Bbx.Features.PullRequests.PullRequestConflicts;

public sealed record PullRequestConflictsRequest(string? Workspace, string? Repository, int Id, int Limit);
