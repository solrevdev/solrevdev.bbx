using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.MergePullRequest;

public sealed class MergePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(MergePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object>
        {
            ["type"] = "pullrequest",
            ["merge_strategy"] = request.Strategy,
            ["close_source_branch"] = request.CloseSourceBranch,
        };
        if (!string.IsNullOrEmpty(request.Message)) body["message"] = request.Message;

        return await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/merge", body, ct);
    }
}
