namespace Bbx.Features.Commits.CommitPatch;

public sealed record CommitPatchRequest(string? Workspace, string? Repository, string Hash);
