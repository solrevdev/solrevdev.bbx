namespace Bbx.Features.Snippets.SnippetFiles;

public sealed record SnippetFilesRequest(
    string SnippetId,
    string? FileName,
    string? Workspace,
    bool Raw,
    string? Revision = null);
