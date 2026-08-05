namespace Bbx.Features.Snippets;

internal static class SnippetPath
{
    /// <summary>
    /// A snippet's base path, pinned to <paramref name="revision"/> when one is
    /// given. Bitbucket takes the revision as an extra path segment rather than
    /// a query parameter, and uses it for optimistic concurrency on the writes:
    /// an update against a stale revision is refused instead of clobbering.
    /// </summary>
    public static string For(string workspace, string snippetId, string? revision) =>
        string.IsNullOrEmpty(revision)
            ? $"snippets/{workspace}/{snippetId}"
            : $"snippets/{workspace}/{snippetId}/{Uri.EscapeDataString(revision)}";
}
