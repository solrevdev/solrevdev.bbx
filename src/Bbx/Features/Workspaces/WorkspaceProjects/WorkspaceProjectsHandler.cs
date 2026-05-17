using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.WorkspaceProjects;

public sealed class WorkspaceProjectsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(WorkspaceProjectsRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (!string.IsNullOrEmpty(request.View))
        {
            var project = await client.GetAsync<JsonElement>($"workspaces/{workspace}/projects/{request.View}", ct);
            return new
            {
                key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
                uuid = project.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                updated_on = project.TryGetProperty("updated_on", out var up) ? up.GetString() : null,
            };
        }

        if (!string.IsNullOrEmpty(request.Create))
        {
            if (string.IsNullOrEmpty(request.Key))
                throw new BbxUserException("Error: --key is required when creating a project.");

            var payload = new Dictionary<string, object>
            {
                ["name"] = request.Create,
                ["key"] = request.Key,
                ["is_private"] = request.IsPrivate ?? true,
            };

            if (!string.IsNullOrEmpty(request.Description))
                payload["description"] = request.Description;

            var project = await client.PostAsync<JsonElement>($"workspaces/{workspace}/projects", payload, ct);

            return new
            {
                key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            };
        }

        if (request.Delete)
        {
            if (string.IsNullOrEmpty(request.Key))
                throw new BbxUserException("Error: --key is required when deleting a project.");

            await client.DeleteAsync($"workspaces/{workspace}/projects/{request.Key}", ct);
            return new { deleted = true, project_key = request.Key };
        }

        var projects = new List<object>();
        await foreach (var project in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/projects", ct))
        {
            projects.Add(new
            {
                key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
                is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            });

            if (projects.Count >= request.Limit) break;
        }

        return new { projects, count = projects.Count };
    }
}
