namespace Bbx.Features.Snippets.SnippetComments;

public sealed record SnippetCommentsRequest(
    string SnippetId,
    string? Workspace,
    string? AddContent,
    int? DeleteId,
    int? UpdateId = null,
    string? UpdateContent = null,
    int? ViewId = null);
