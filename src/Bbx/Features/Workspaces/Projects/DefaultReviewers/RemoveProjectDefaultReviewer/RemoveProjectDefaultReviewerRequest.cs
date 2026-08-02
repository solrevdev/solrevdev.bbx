namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.RemoveProjectDefaultReviewer;

public sealed record RemoveProjectDefaultReviewerRequest(
    string? Workspace,
    string ProjectKey,
    string Target);
