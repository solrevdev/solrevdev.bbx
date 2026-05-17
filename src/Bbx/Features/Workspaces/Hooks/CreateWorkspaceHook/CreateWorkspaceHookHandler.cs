using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Hooks.CreateWorkspaceHook;

public sealed class CreateWorkspaceHookHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(CreateWorkspaceHookRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var payload = new Dictionary<string, object>
        {
            ["url"] = request.Url,
            ["active"] = request.Active,
            ["events"] = request.Events is { Length: > 0 } ? request.Events : new[] { "repo:push" },
        };
        if (!string.IsNullOrEmpty(request.Description))
            payload["description"] = request.Description;

        var hook = await client.PostAsync<JsonElement>(
            $"workspaces/{workspace}/hooks", payload, ct);

        return new
        {
            uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
            description = hook.TryGetProperty("description", out var d) ? d.GetString() : null,
            url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
            active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
            events = ExtractEvents(hook),
            created_at = hook.TryGetProperty("created_at", out var c) ? c.GetString() : null,
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
