namespace Bbx.Features.Commits.ListCommitComments;

public sealed record ListCommitCommentsRequest(string? Workspace, string? Repository, string Hash);
