using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Tasks.ListPullRequestTasks;

public sealed class ListPullRequestTasksHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPullRequestTasksRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var tasks = new List<object>();
        var count = 0;
        await foreach (var task in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/tasks", ct))
        {
            tasks.Add(new
            {
                id = task.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.Number
                    ? i.GetInt32()
                    : (int?)null,
                state = task.TryGetProperty("state", out var s) ? s.GetString() : null,
                content = task.TryGetObject("content", out var c) && c.TryGetProperty("raw", out var raw)
                    ? raw.GetString()
                    : null,
                created_on = task.TryGetProperty("created_on", out var co) ? co.GetString() : null,
                updated_on = task.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
                creator = task.TryGetObject("creator", out var cr) && cr.TryGetProperty("display_name", out var dn)
                    ? dn.GetString()
                    : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { pull_request_id = request.Id, count = tasks.Count, tasks };
    }
}
