namespace Bbx.Features.Commits.ViewCommit;

public sealed record ViewCommitRequest(string? Workspace, string? Repository, string Hash);
