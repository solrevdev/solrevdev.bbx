using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.WorkspaceHooks;

public sealed class WorkspaceHooksHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(WorkspaceHooksRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (!string.IsNullOrEmpty(request.View))
        {
            var hook = await client.GetAsync<JsonElement>($"workspaces/{workspace}/hooks/{request.View}", ct);
            var hookEvents = ExtractEvents(hook);

            return new
            {
                uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                description = hook.TryGetProperty("description", out var d) ? d.GetString() : null,
                url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
                active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
                events = hookEvents,
                created_at = hook.TryGetProperty("created_at", out var c) ? c.GetString() : null,
            };
        }

        if (!string.IsNullOrEmpty(request.Create))
        {
            var payload = new Dictionary<string, object>
            {
                ["url"] = request.Create,
                ["active"] = request.Active ?? true,
                ["events"] = request.Events ?? new[] { "repo:push" },
            };

            if (!string.IsNullOrEmpty(request.Description))
                payload["description"] = request.Description;

            var hook = await client.PostAsync<JsonElement>($"workspaces/{workspace}/hooks", payload, ct);

            return new
            {
                uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
                active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
                created_at = hook.TryGetProperty("created_at", out var c) ? c.GetString() : null,
            };
        }

        if (!string.IsNullOrEmpty(request.DeleteUuid))
        {
            await client.DeleteAsync($"workspaces/{workspace}/hooks/{request.DeleteUuid}", ct);
            return new { deleted = true, uuid = request.DeleteUuid };
        }

        var hooks = new List<object>();
        await foreach (var hook in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/hooks", ct))
        {
            hooks.Add(new
            {
                uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                description = hook.TryGetProperty("description", out var d) ? d.GetString() : null,
                url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
                active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
                events = ExtractEvents(hook),
            });

            if (hooks.Count >= request.Limit) break;
        }

        return new { hooks, count = hooks.Count };
    }

    private static List<string> ExtractEvents(JsonElement hook)
    {
        var hookEvents = new List<string>();
        if (hook.TryGetProperty("events", out var e) && e.ValueKind == JsonValueKind.Array)
        {
            foreach (var ev in e.EnumerateArray())
            {
                if (ev.GetString() is string evStr)
                    hookEvents.Add(evStr);
            }
        }
        return hookEvents;
    }
}
