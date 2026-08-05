namespace Bbx.Features.PullRequests.UpdatePullRequest;

/// <summary>
/// A partial update. Every optional field is null when the caller left the
/// matching switch off, and only non-null fields reach the wire.
/// </summary>
public sealed record UpdatePullRequestRequest(
    string? Workspace,
    string? Repository,
    int Id,
    string? Title,
    string? Body,
    string? Destination,
    string[]? Reviewers,
    bool? CloseSourceBranch);
