namespace Bbx.Features.Commits.CommitDiffstat;

public sealed record CommitDiffstatRequest(string? Workspace, string? Repository, string Spec, int Limit);
