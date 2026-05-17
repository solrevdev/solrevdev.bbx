namespace Bbx.Features.Source.LsSource;

public sealed record LsSourceRequest(string? Workspace, string? Repository, string Ref, string? Path, int Limit);
