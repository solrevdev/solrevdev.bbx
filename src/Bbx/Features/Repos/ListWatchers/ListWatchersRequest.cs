namespace Bbx.Features.Repos.ListWatchers;

public sealed record ListWatchersRequest(string? Workspace, string? Repository, int Limit);
