namespace Bbx.Features.Downloads.UploadDownload;

public sealed record UploadDownloadRequest(string? Workspace, string? Repository, string FilePath, string? Name);
