using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Repos.DefaultReviewers.ListDefaultReviewers;

namespace Bbx.Features.Repos.DefaultReviewers.ViewDefaultReviewer;

public sealed class ViewDefaultReviewerHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewDefaultReviewerRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var reviewer = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/default-reviewers/{Uri.EscapeDataString(request.Target)}", ct);
        return ListDefaultReviewersHandler.ProjectReviewer(reviewer);
    }
}
