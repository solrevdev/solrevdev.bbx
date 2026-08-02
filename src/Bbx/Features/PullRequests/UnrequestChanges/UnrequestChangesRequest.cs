namespace Bbx.Features.PullRequests.UnrequestChanges;

public sealed record UnrequestChangesRequest(string? Workspace, string? Repository, int Id);
