namespace Bbx.Features.Repos.UpdateRepo;

/// <summary>
/// A partial update. Every field is null when the caller left the matching
/// switch off, and only non-null fields reach the wire.
/// </summary>
public sealed record UpdateRepoRequest(
    string? Workspace,
    string Repository,
    string? Name,
    string? Description,
    bool? IsPrivate,
    string? ForkPolicy,
    string? Language,
    string? Website,
    string? Project,
    string? MainBranch,
    bool? HasIssues,
    bool? HasWiki);
