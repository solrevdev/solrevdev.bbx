namespace Bbx.Features.Commits.UnapproveCommit;

public sealed record UnapproveCommitRequest(string? Workspace, string? Repository, string Hash);
