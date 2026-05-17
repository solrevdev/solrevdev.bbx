namespace Bbx.Features.PullRequests.PullRequestPatch;

public sealed record PullRequestPatchRequest(string? Workspace, string? Repository, int Id);
