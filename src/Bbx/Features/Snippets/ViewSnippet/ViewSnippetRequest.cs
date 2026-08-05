namespace Bbx.Features.Snippets.ViewSnippet;

public sealed record ViewSnippetRequest(string SnippetId, string? Workspace, string? Revision = null);
