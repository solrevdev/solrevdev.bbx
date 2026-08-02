namespace Bbx.Features.Repos.DefaultReviewers.RemoveDefaultReviewer;

public sealed record RemoveDefaultReviewerRequest(string? Workspace, string? Repository, string Target);
