namespace Bbx.Features.Commits.MergeBase;

public sealed record MergeBaseRequest(string? Workspace, string? Repository, string Spec);
