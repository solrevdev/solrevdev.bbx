namespace Bbx.Features.Snippets.SnippetCommits;

/// <summary>
/// With <paramref name="Revision"/> set, one commit rather than the log.
/// </summary>
public sealed record SnippetCommitsRequest(
    string SnippetId,
    string? Workspace,
    string? Revision,
    int Limit);
