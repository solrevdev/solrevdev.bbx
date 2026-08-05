namespace Bbx.Features.Workspaces.ListWorkspacePullRequests;

public sealed record ListWorkspacePullRequestsRequest(
    string? Workspace,
    string User,
    string? State,
    int Limit);
