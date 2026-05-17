namespace Bbx.Features.Commits.ListCommitStatuses;

public sealed record ListCommitStatusesRequest(string? Workspace, string? Repository, string Hash);
