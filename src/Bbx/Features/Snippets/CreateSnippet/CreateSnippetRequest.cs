namespace Bbx.Features.Snippets.CreateSnippet;

public sealed record CreateSnippetRequest(string Title, string[] Files, bool IsPrivate, string? Workspace);
