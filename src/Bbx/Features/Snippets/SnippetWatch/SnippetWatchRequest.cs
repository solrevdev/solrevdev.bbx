namespace Bbx.Features.Snippets.SnippetWatch;

public sealed record SnippetWatchRequest(string SnippetId, string? Workspace, bool List, bool Unwatch);
