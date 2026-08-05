using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ListWorkspacePullRequests;

public sealed class ListWorkspacePullRequestsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWorkspacePullRequestsRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        // Every pull request the user authored or was asked to review, across
        // the whole workspace. `pr list` only sees one repository.
        var endpoint = $"workspaces/{workspace}/pullrequests/{Uri.EscapeDataString(request.User)}";
        if (!string.IsNullOrEmpty(request.State))
            endpoint += $"?state={Uri.EscapeDataString(request.State.ToUpperInvariant())}";

        var pullRequests = new List<object>();
        var count = 0;
        await foreach (var pr in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            pullRequests.Add(new
            {
                id = pr.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number ? id.GetInt32() : 0,
                title = pr.GetStringOrNull("title"),
                state = pr.GetStringOrNull("state"),
                repository = pr.TryGetObject("destination", out var d) && d.TryGetObject("repository", out var r)
                    ? r.GetStringOrNull("full_name") : null,
                author = pr.TryGetObject("author", out var a) ? a.GetStringOrNull("display_name") : null,
                updated_on = pr.GetStringOrNull("updated_on"),
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace, user = request.User, count = pullRequests.Count, pull_requests = pullRequests };
    }
}
