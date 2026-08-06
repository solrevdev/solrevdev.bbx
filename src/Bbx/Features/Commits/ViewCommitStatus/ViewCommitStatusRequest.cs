namespace Bbx.Features.Commits.ViewCommitStatus;

public sealed record ViewCommitStatusRequest(string? Workspace, string? Repository, string Hash, string Key);
