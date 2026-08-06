namespace Bbx.Features.Repos.FileConflicts;

public sealed record FileConflictsRequest(string? Workspace, string? Repository, string Spec, int Limit);
