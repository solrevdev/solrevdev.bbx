namespace Bbx.Features.Repos.DefaultReviewers.AddDefaultReviewer;

public sealed record AddDefaultReviewerRequest(string? Workspace, string? Repository, string Target);
