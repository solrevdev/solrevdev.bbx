using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.DefaultReviewers.ListProjectDefaultReviewers;

public sealed class ListProjectDefaultReviewersHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListProjectDefaultReviewersRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");

        var reviewers = new List<object>();
        var count = 0;
        await foreach (var r in client.GetPaginatedAsync<JsonElement>(
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/default-reviewers", ct))
        {
            reviewers.Add(new
            {
                uuid = r.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                account_id = r.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                display_name = r.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                type = r.TryGetProperty("type", out var t) ? t.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            project_key = request.ProjectKey,
            count = reviewers.Count,
            reviewers,
        };
    }
}
