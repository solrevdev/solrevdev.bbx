using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.RequestChanges;

public sealed class RequestChangesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RequestChangesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.PostAsync<object>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/request-changes", null, ct);
        return $"✓ Requested changes on PR #{request.Id}";
    }
}
