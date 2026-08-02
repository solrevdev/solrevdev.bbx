using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Hooks.UpdateWorkspaceHook;

public sealed class UpdateWorkspaceHookHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(UpdateWorkspaceHookRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (string.IsNullOrEmpty(request.Url)
            && string.IsNullOrEmpty(request.Description)
            && (request.Events is null || request.Events.Length == 0)
            && request.Active is null)
        {
            throw new BbxUserException(
                "Error: Provide at least one of --url, --description, --events, or --active to update.");
        }

        var payload = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.Url)) payload["url"] = request.Url;
        if (!string.IsNullOrEmpty(request.Description)) payload["description"] = request.Description;
        if (request.Events is { Length: > 0 }) payload["events"] = request.Events;
        if (request.Active is { } active) payload["active"] = active;

        var hook = await client.PutAsync<JsonElement>(
            $"workspaces/{workspace}/hooks/{Uri.EscapeDataString(request.Uid)}", payload, ct);

        return new
        {
            uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
            description = hook.TryGetProperty("description", out var d) ? d.GetString() : null,
            url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
            active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
            events = ExtractEvents(hook),
        };
    }

    private static List<string> ExtractEvents(JsonElement hook)
    {
        var list = new List<string>();
        if (hook.TryGetProperty("events", out var e) && e.ValueKind == JsonValueKind.Array)
        {
            foreach (var ev in e.EnumerateArray())
            {
                if (ev.GetString() is string s) list.Add(s);
            }
        }
        return list;
    }
}
