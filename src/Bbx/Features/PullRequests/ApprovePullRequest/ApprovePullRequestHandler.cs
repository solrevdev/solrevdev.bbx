using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.ApprovePullRequest;

public sealed class ApprovePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(ApprovePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/approve", null, ct);
        return $"✓ Approved PR #{request.Id}";
    }
}
