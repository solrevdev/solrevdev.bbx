namespace Bbx.Features.Repos.DefaultReviewers.ListDefaultReviewers;

public sealed record ListDefaultReviewersRequest(string? Workspace, string? Repository, int Limit);
