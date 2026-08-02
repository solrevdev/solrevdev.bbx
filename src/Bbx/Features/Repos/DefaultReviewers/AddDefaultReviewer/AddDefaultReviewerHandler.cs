using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DefaultReviewers.AddDefaultReviewer;

public sealed class AddDefaultReviewerHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddDefaultReviewerRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrEmpty(request.Target))
            throw new BbxUserException("Error: --target (account ID or UUID) is required.");

        // PUT with an empty body adds the reviewer; Bitbucket accepts no payload here.
        var reviewer = await client.PutAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/default-reviewers/{Uri.EscapeDataString(request.Target)}",
            new { }, ct);

        return new
        {
            added = true,
            target = request.Target,
            reviewer = reviewer.ValueKind == JsonValueKind.Undefined ? null : (object)reviewer,
        };
    }
}
