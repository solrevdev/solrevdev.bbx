namespace Bbx.Features.PullRequests.PullRequestMergeStatus;

public sealed record PullRequestMergeStatusRequest(string? Workspace, string? Repository, int Id, string TaskId);
