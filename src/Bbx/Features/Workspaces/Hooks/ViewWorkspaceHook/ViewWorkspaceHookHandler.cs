using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Hooks.ViewWorkspaceHook;

public sealed class ViewWorkspaceHookHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewWorkspaceHookRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var hook = await client.GetAsync<JsonElement>(
            $"workspaces/{workspace}/hooks/{Uri.EscapeDataString(request.Uid)}", ct);

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
