using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.CreatePullRequest;

public sealed class CreatePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(CreatePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object>
        {
            ["title"] = request.Title,
            ["source"] = new { branch = new { name = request.Source } },
            ["destination"] = new { branch = new { name = request.Destination } },
            ["close_source_branch"] = request.CloseSourceBranch,
        };

        if (!string.IsNullOrEmpty(request.Body)) body["description"] = request.Body;
        if (request.Reviewers?.Length > 0)
            body["reviewers"] = request.Reviewers.Select(r => new { account_id = r }).ToArray();

        return await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/pullrequests", body, ct);
    }
}
