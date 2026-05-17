using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.AddProjectDefaultReviewer;

public sealed class AddProjectDefaultReviewerHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddProjectDefaultReviewerRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        if (string.IsNullOrEmpty(request.Target))
            throw new BbxUserException("Error: --target (account ID or UUID) is required.");

        var reviewer = await client.PutAsync<JsonElement>(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/default-reviewers/{Uri.EscapeDataString(request.Target)}",
            new { }, ct);

        return new
        {
            added = true,
            target = request.Target,
            reviewer = reviewer.ValueKind == JsonValueKind.Undefined ? null : (object)reviewer,
        };
    }
}
