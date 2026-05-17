namespace Bbx.Features.PullRequests.PullRequestStatuses;

public sealed record PullRequestStatusesRequest(string? Workspace, string? Repository, int Id);
