namespace Bbx.Features.Source.CatSource;

public sealed record CatSourceRequest(string? Workspace, string? Repository, string Ref, string Path);
