using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Tasks.AddPullRequestTask;

public sealed class AddPullRequestTaskHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddPullRequestTaskRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (string.IsNullOrEmpty(request.Content))
            throw new BbxUserException("Error: --content (task body) is required.");

        var body = new { content = new { raw = request.Content } };
        var task = await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/tasks", body, ct);

        return new
        {
            id = task.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.Number
                ? i.GetInt32()
                : (int?)null,
            state = task.TryGetProperty("state", out var s) ? s.GetString() : null,
            content = task.TryGetProperty("content", out var c) && c.TryGetProperty("raw", out var raw)
                ? raw.GetString()
                : null,
            created_on = task.TryGetProperty("created_on", out var co) ? co.GetString() : null,
        };
    }
}
