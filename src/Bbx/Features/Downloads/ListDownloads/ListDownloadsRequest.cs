namespace Bbx.Features.Downloads.ListDownloads;

public sealed record ListDownloadsRequest(string? Workspace, string? Repository, int Limit);
