namespace Bbx.Features.Commits.FileHistory;

public sealed record FileHistoryRequest(string? Workspace, string? Repository, string Hash, string Path, int Limit);
