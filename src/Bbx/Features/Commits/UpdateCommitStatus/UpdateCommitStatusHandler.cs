using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.UpdateCommitStatus;

public sealed class UpdateCommitStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    private static readonly HashSet<string> AllowedStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUCCESSFUL", "FAILED", "INPROGRESS", "STOPPED",
    };

    public async Task<object> HandleAsync(UpdateCommitStatusRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (string.IsNullOrEmpty(request.Key))
            throw new BbxUserException("Error: --key is required.");

        if (string.IsNullOrEmpty(request.State)
            && string.IsNullOrEmpty(request.Url)
            && string.IsNullOrEmpty(request.Name)
            && string.IsNullOrEmpty(request.Description))
        {
            throw new BbxUserException(
                "Error: Provide at least one of --state, --url, --name, or --description to update.");
        }

        if (!string.IsNullOrEmpty(request.State) && !AllowedStates.Contains(request.State))
            throw new BbxUserException(
                "Error: --state must be one of SUCCESSFUL, FAILED, INPROGRESS, STOPPED.");

        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.State)) body["state"] = request.State.ToUpperInvariant();
        if (!string.IsNullOrEmpty(request.Url)) body["url"] = request.Url;
        if (!string.IsNullOrEmpty(request.Name)) body["name"] = request.Name;
        if (!string.IsNullOrEmpty(request.Description)) body["description"] = request.Description;

        var status = await client.PutAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/commit/{request.Hash}/statuses/build/{Uri.EscapeDataString(request.Key)}",
            body, ct);

        return new
        {
            key = status.TryGetProperty("key", out var k) ? k.GetString() : request.Key,
            state = status.TryGetProperty("state", out var st) ? st.GetString() : null,
            name = status.TryGetProperty("name", out var n) ? n.GetString() : null,
            url = status.TryGetProperty("url", out var u) ? u.GetString() : null,
            description = status.TryGetProperty("description", out var d) ? d.GetString() : null,
            updated_on = status.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
        };
    }
}
