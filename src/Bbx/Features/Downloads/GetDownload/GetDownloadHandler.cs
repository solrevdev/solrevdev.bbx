using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Downloads.GetDownload;

public sealed class GetDownloadHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task HandleAsync(GetDownloadRequest request, Stream destination, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Filename))
            throw new BbxUserException("Error: <filename> is required.");

        await client.CopyToAsync(
            $"/repositories/{ws}/{repo}/downloads/{Uri.EscapeDataString(request.Filename)}", destination, ct);
    }
}
