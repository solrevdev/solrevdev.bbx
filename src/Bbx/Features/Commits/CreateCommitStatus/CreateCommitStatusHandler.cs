using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.CreateCommitStatus;

public sealed class CreateCommitStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    private static readonly HashSet<string> AllowedStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUCCESSFUL", "FAILED", "INPROGRESS", "STOPPED",
    };

    public async Task<object> HandleAsync(CreateCommitStatusRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (string.IsNullOrEmpty(request.Key))
            throw new BbxUserException("Error: --key is required.");
        if (string.IsNullOrEmpty(request.State) || !AllowedStates.Contains(request.State))
            throw new BbxUserException(
                "Error: --state must be one of SUCCESSFUL, FAILED, INPROGRESS, STOPPED.");
        if (string.IsNullOrEmpty(request.Url))
            throw new BbxUserException("Error: --url is required.");

        var body = new Dictionary<string, object>
        {
            ["key"] = request.Key,
            ["state"] = request.State.ToUpperInvariant(),
            ["url"] = request.Url,
        };
        if (!string.IsNullOrEmpty(request.Name)) body["name"] = request.Name;
        if (!string.IsNullOrEmpty(request.Description)) body["description"] = request.Description;

        var status = await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/statuses/build", body, ct);

        return Project(status);
    }

    private static object Project(JsonElement s) => new
    {
        key = s.TryGetProperty("key", out var k) ? k.GetString() : null,
        state = s.TryGetProperty("state", out var st) ? st.GetString() : null,
        name = s.TryGetProperty("name", out var n) ? n.GetString() : null,
        url = s.TryGetProperty("url", out var u) ? u.GetString() : null,
        description = s.TryGetProperty("description", out var d) ? d.GetString() : null,
        created_on = s.TryGetProperty("created_on", out var co) ? co.GetString() : null,
        updated_on = s.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
    };
}
