namespace Bbx.Features.PullRequests.Tasks.ListPullRequestTasks;

public sealed record ListPullRequestTasksRequest(string? Workspace, string? Repository, int Id, int Limit);
