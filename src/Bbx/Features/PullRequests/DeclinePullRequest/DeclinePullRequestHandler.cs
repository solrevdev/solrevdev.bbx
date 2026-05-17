using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.DeclinePullRequest;

public sealed class DeclinePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeclinePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var body = !string.IsNullOrEmpty(request.Reason) ? new { reason = request.Reason } : null;
        await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/decline", body, ct);
        return $"✓ Declined PR #{request.Id}";
    }
}
