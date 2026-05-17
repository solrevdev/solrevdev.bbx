using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.UnrequestChanges;

public sealed class UnrequestChangesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(UnrequestChangesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/request-changes", ct);
        return $"✓ Removed change request on PR #{request.Id}";
    }
}
