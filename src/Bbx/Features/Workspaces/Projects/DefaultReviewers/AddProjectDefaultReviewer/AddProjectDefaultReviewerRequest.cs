namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.AddProjectDefaultReviewer;

public sealed record AddProjectDefaultReviewerRequest(
    string? Workspace,
    string ProjectKey,
    string Target);
