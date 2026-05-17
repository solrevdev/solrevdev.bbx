namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.ListProjectDefaultReviewers;

public sealed record ListProjectDefaultReviewersRequest(string? Workspace, string ProjectKey, int Limit);
