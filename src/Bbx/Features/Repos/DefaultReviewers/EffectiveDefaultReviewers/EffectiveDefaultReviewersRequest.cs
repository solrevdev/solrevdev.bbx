namespace Bbx.Features.Repos.DefaultReviewers.EffectiveDefaultReviewers;

public sealed record EffectiveDefaultReviewersRequest(string? Workspace, string? Repository, int Limit);
