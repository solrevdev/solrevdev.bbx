using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Downloads.UploadDownload;

public sealed class UploadDownloadHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(UploadDownloadRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (string.IsNullOrEmpty(request.FilePath))
            throw new BbxUserException("Error: --file is required.");
        if (!File.Exists(request.FilePath))
            throw new BbxUserException($"Error: File not found: {request.FilePath}");

        var name = string.IsNullOrEmpty(request.Name) ? Path.GetFileName(request.FilePath) : request.Name;
        var bytes = await File.ReadAllBytesAsync(request.FilePath, ct);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "files", name);

        // Successful upload returns 201 with no body.
        await client.PostMultipartAsync<object>($"/repositories/{ws}/{repo}/downloads", content, ct);

        return new
        {
            workspace = ws,
            repository = repo,
            name,
            size = bytes.LongLength,
            local_path = request.FilePath,
        };
    }
}
