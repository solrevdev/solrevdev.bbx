namespace Bbx.Features.Downloads.GetDownload;

public sealed record GetDownloadRequest(string? Workspace, string? Repository, string Filename, string? Output);
