namespace Bbx.Features.Snippets.ListSnippets;

public sealed record ListSnippetsRequest(string? Workspace, string? Role, int Limit);
