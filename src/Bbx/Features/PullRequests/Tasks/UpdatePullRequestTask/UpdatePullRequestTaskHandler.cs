using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.Tasks.UpdatePullRequestTask;

public sealed class UpdatePullRequestTaskHandler(BitbucketClient client, CredentialManager credentials)
{
    private static readonly HashSet<string> AllowedStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "RESOLVED", "UNRESOLVED",
    };

    public async Task<object> HandleAsync(UpdatePullRequestTaskRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (string.IsNullOrEmpty(request.Content) && string.IsNullOrEmpty(request.State))
            throw new BbxUserException("Error: Provide --content or --state to update.");
        if (!string.IsNullOrEmpty(request.State) && !AllowedStates.Contains(request.State))
            throw new BbxUserException("Error: --state must be one of RESOLVED, UNRESOLVED.");

        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.Content)) body["content"] = new { raw = request.Content };
        if (!string.IsNullOrEmpty(request.State)) body["state"] = request.State.ToUpperInvariant();

        var task = await client.PutAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/tasks/{request.TaskId}", body, ct);

        return new
        {
            id = task.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.Number
                ? i.GetInt32()
                : request.TaskId,
            state = task.TryGetProperty("state", out var s) ? s.GetString() : null,
            content = task.TryGetObject("content", out var c) && c.TryGetProperty("raw", out var raw)
                ? raw.GetString()
                : null,
            updated_on = task.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
        };
    }
}
