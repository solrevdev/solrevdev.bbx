using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Downloads.DeleteDownload;

public sealed class DeleteDownloadHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteDownloadRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/downloads/{Uri.EscapeDataString(request.Filename)}", ct);
        return $"✓ Deleted download '{request.Filename}' from {ws}/{repo}";
    }
}
