namespace Bbx.Features.Source.LsSource;

/// <summary>
/// <paramref name="Ref"/> is null when the caller did not name one, which lists
/// the root of the main branch without having to know its name.
/// </summary>
public sealed record LsSourceRequest(string? Workspace, string? Repository, string? Ref, string? Path, int Limit);
