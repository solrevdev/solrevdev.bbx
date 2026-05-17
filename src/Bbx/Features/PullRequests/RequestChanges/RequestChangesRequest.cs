namespace Bbx.Features.PullRequests.RequestChanges;

public sealed record RequestChangesRequest(string? Workspace, string? Repository, int Id);
