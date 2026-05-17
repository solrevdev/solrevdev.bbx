namespace Bbx.Features.Repos.ListForks;

public sealed record ListForksRequest(string? Workspace, string? Repository, int Limit);
