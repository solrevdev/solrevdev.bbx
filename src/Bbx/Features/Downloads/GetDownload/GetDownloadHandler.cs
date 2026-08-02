using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Downloads.GetDownload;

public sealed class GetDownloadHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<byte[]> HandleAsync(GetDownloadRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Filename))
            throw new BbxUserException("Error: <filename> is required.");

        return await client.GetByteArrayAsync(
            $"/repositories/{ws}/{repo}/downloads/{Uri.EscapeDataString(request.Filename)}", ct);
    }
}
