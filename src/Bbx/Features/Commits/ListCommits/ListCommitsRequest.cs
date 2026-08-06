namespace Bbx.Features.Commits.ListCommits;

/// <summary>
/// <paramref name="Include"/> and <paramref name="Exclude"/> name refs to walk
/// from and to stop at. Bitbucket only accepts them on the POST form of the
/// endpoint, so passing either switches the verb.
/// </summary>
public sealed record ListCommitsRequest(
    string? Workspace,
    string? Repository,
    string? Branch,
    string? Path,
    int Limit,
    string[]? Include = null,
    string[]? Exclude = null);
