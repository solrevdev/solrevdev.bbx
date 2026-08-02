namespace Bbx.Features.Commits.ApproveCommit;

public sealed record ApproveCommitRequest(string? Workspace, string? Repository, string Hash);
