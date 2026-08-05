namespace Bbx.Features.Snippets.SnippetDiff;

/// <summary>
/// Diff and patch are the same resource in two formats, so one request carries
/// both.
/// </summary>
public sealed record SnippetDiffRequest(
    string SnippetId,
    string? Workspace,
    string Revision,
    bool AsPatch);
