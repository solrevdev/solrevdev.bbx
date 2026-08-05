namespace Bbx.Features.Snippets.UpdateSnippet;

public sealed record UpdateSnippetRequest(
    string SnippetId,
    string? Title,
    string[]? Files,
    bool? IsPrivate,
    string? Workspace,
    string? Revision = null);
