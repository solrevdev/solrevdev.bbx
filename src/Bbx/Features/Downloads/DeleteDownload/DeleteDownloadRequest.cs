namespace Bbx.Features.Downloads.DeleteDownload;

public sealed record DeleteDownloadRequest(string? Workspace, string? Repository, string Filename);
