namespace Bbx.Features.Commits.CommitDiff;

public sealed record CommitDiffRequest(string? Workspace, string? Repository, string Hash);
