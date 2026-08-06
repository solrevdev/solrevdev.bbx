namespace Bbx.Features.Branches.ListRefs;

public sealed record ListRefsRequest(string? Workspace, string? Repository, string? Query, int Limit);
